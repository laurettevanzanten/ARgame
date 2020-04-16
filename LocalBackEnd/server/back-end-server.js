const seedrandom = require('seedrandom');

var UserCredentials = require("./user-credentials.js");
var WebOperation = require("./web-operation.js");
var LoginResponse = require("./login-response.js");

// token can only live for 5 minutes
const maxTokenAge = 60 * 5;

// string length of a token
const maxTokenLength = 8;

var _userCredentials = {};

// how often do we flush the read/write quuees 
var _flushTimeout = 1000;

// queue containing write ops
var _writeQueue = [];

// queue containing read ops
var _readQueue = [];

// currently active queue (either _readQueue or _writeQueue)
var _activeQueue;

var _logger;
var _sessionDb;

/**
 * Configure the back-end server. The config is required to contain a 'database' and optionally a 'logger'
 * and 'timeout' setting. Although this approach provides the start of decoupling the database implementation from
 * its use in this class, the current implementation still very much assumes its using SQLite3.  
 */
exports.config = function (config) {
	if (config) {	
		_logger = config.logger || {
			info : (str) => {},
			error : (str) => {}
		};
		_sessionDb = config.database;
		_flushTimeout = config.timeout || 1000;
	}
}

/**
 * Start the back end server
 */
exports.start = function() {
	_activeQueue = _readQueue;
	setTimeout(updateQueues, 2000);
}

/**
 * Replies with a default webpage to render on a get.
 * @param response 
 */
exports.renderDefaultWebPage = function (response)  {
	response.send('Laurette - Server, thesis v0.001');
}

/** 
 * Logs the user in based off a user and password (plain text -- we're _really_ not expecting the game to
 * be hacked) 
 * @param request httpRequest made by the client, must contain a body.user and body.password
 * @param reponse httpResponse object used to respond to the client.
 */
exports.login = function (request, response) {
	_logger.info(getSource(request) + " logging in as " + request.body.user + ", " + request.body.password); 
	_readQueue.push( new WebOperation("login",  request,  response ));
}

/** 
 * Logs the user out, removing its credits directly. Sends an empty response.
 * @param request httpRequest made by the client, must contain a body.token 
 * @param reponse httpResponse object used to respond to the client.
 */
exports.logout = function (request, response) {

 	_logger.info(getSource(request) + " logging out with " + request.body.token); 

	if (_userCredentials[request.body.token]) {
		delete _userCredentials[request.body.token];
	} 

	response.end();
}

/*
 * Handle a order post. The request body should have a token provided by the login calls a timestamp
 * (to ack), a session id and items.
 */
exports.postOrder = function (request, response) {

	_logger.info(getSource(request) + " post order with token = " + request.body.token); 
	
	var credentials  = _userCredentials[request.body.token];
	
	if (request && request.body && credentials) {
		// get the age of the token and see if it is still valid
		var now = new Date();
		var credentialsAge = (now.getTime() - credentials.date.getTime()) / 1000;
		
		if (credentialsAge < maxTokenAge) {
			
			// update the token's life
			_userCredentials[request.body.token].date = now;

			var valuesErrors = validateOrderProperties(request.body.sessionId, request.body.timeStamp, request.body.items);

			if (!valuesErrors) {
				if (request.body.items && request.body.items.length > 0) {
					_writeQueue.push(new WebOperation("insert-order", request, response));
				} else {
					// empty array - don't bother inserting empty sets
					sendAck(response, request.body.timeStamp);
				}
			} else {
				sendErr(request, response, "post order with invalid values " + valuesErrors);
			}

		} else {
			sendErr(request, response, "post order with outdated token, credentials=" + JSON.stringify(credentials));
			delete _userCredentials[request.body.token];
		}
	} else {
		sendErr(request, response, "invalid request or token provided (token="+ request.body.token +").");
	}
}

// --- PRIVATE FUNCTIONS ----------------------------------------------------------------------------------------------

/** 
 * The back-end server has two queues, a read queue and a write queue. The read queue contains all read operations 
 * (ie. only select at this point), the write queue contains all the write operations (ie insert). The seperation of 
 * read and write queues were originally used to get around specific db locks and optimize operations. 
 */
function updateQueues() {

	if (_activeQueue === _readQueue) {
		var temp = _readQueue;
		_readQueue = [];
		flushReadQueue(temp, () => {
			_activeQueue = _writeQueue;
			setTimeout(updateQueues, _flushTimeout );
		});
	} else {
		var temp = _writeQueue;
		_writeQueue = [];
		flushWriteQueue(temp, () => {
			_activeQueue = _readQueue;
			setTimeout(updateQueues, _flushTimeout );
		});
	}
}

/**
 * Start all the (async) read operations.
 * @param queue an array of http operations 
 * @param onCompleteCallback a callback when all operations are complete with the signature void op()
 */
function flushReadQueue(queue, onCompleteCallback) {

	if (queue.length > 0) {
		var outstandingOperations = queue.length;

		for (var i = 0; i < queue.length; i++) {
			const request = queue[i].request;
			const response = queue[i].response;
			
			login(request.body.user, request.body.password, (errCode, message) => {
				
				if (errCode === 0) {
					response.send(message);
				} else {
					sendErr(request, response, message);
				}
				outstandingOperations--;

				if (outstandingOperations <= 0) {
					onCompleteCallback();
				}
			});
		}
	} else {
		onCompleteCallback();
	}
}

/**
 * start all the write operations. 
 * @param queue an array of http operations 
 * @param onCompleteCallback a callback when all operations are complete with the signature void op()
 */
function flushWriteQueue(queue, onCompleteCallback) {
	if (queue.length === 0) {
		onCompleteCallback();
	}
	else {
		var valuesCollection = [];
		
		// combine all insert operations so we can do with only one insert call
		for (var i = 0; i < queue.length; i++) {
			var msgBody =  queue[i].request.body;
			var itemList = JSON.stringify(msgBody.items);
			var credentials = _userCredentials[msgBody.token];

			valuesCollection.push("(" + credentials.id + "," + msgBody.sessionId + "," + msgBody.timeStamp + ",'" + itemList + "')");			 
		}

		var outstandingOperations = queue.length;

		insertValues(valuesCollection, (err, message) => {		
			for (var i = 0; i < queue.length; i++) {
				var msg = queue[i];

				var res = msg.response;
				var req = msg.request;
				
				if (err) {
					sendErr(req, res, '{"timeStamp": ' + req.body.timeStamp + ', "message": "' + err + '"}');
				} else {
					sendAck(res, req.body.timeStamp );
				}

				outstandingOperations--;

				if (outstandingOperations === 0) {
					onCompleteCallback();
				}
			}
		});
	}
}

/**
 * Send standardize error message via the response
 * @param {*} request 
 * @param {*} response 
 * @param {*} message 
 */
function sendErr(request, response, message) {
	_logger.error(getSource(request) + ", error " + message);
			
	response.statusMessage = message;
	response.status(400).end();
}

/**
 * Send standardize ack message via the response
 * @param {*} response 
 * @param {*} timeStamp 
 */
function sendAck(response, timeStamp) {
	response.send( '{"timeStamp": ' + timeStamp + ', "message": "ack"}' );
}

/**
 * Try to extract the source of request  
 * @param {*} request 
 */
function getSource(request) {
	var originSource = request.headers['x-forwarded-for'];

	if (!originSource) {
		originSource = request.connection.remoteAddress;
	}

	return originSource ? originSource : "unknown";
}

/** Check if the properties of the order are valid. If valid returns an empty string and a non-empty string otherwise */
function validateOrderProperties( sessionId, timeStamp, itemList) {

	return testIsInteger(sessionId, "sessionId") 
			+ testIsInteger(timeStamp, "timeStamp") 
			+ testIsNullOrArray(itemList, "itemList");
}

function testIsNullOrArray(array, propertyName) {

	if (array) {
		if (!Array.isArray(array)) {
			return propertyName + " is not a valid array";
		}
	}

	return "";
}

/*
 * Test if the given value is an integer, if not return a string representing containing the error.
 */
function testIsInteger(value, propertyName) {
	if (value === undefined) {
		return "No " + propertyName + " provided.";
	} else if (typeof(value) !== 'number') {
		return propertyName + " is not a number.";
	} else if (value % 1 !== 0) {
		return propertyName + "  is not an integer.";
	}
	return "";
}


/*
 * Login to the back-end using the given user name and password
 */
function login(name, password, callback) {

	// login the user
	_sessionDb.get("select userId from users where name = ? and password = ?", name, password, (err, row) => {
		if (err ) {
			callback( -1, "db error (err=" +err+ ")." );
		} else if (!row) {
			callback( -1, "user or password " + name + " not found." );
		}  else {	
			const id = 	row.userId;	
			const token = generateToken(id, name, password);

			_logger.info("assiging token " + token + " to " + name);
			
			_userCredentials[token] = new UserCredentials(id, token, new Date());

			tryRetrieveSessionAndTimeStamp(id, token, callback);
		}
	});
}

function tryRetrieveSessionAndTimeStamp(userId, userToken, callback) {
	// get the last session the user was working on
	_sessionDb.get("select max(session) from sessions where userId = ?", userId, (maxSessionErr, maxSessionRow) => {
		if (maxSessionErr) {
			callback( -1, "error while retrieving max session (err=" + maxSessionErr + ")." );
		} else { 
			// note the syntac assumes a SQLite3 response 
			var maxSession = maxSessionRow["max(session)"];

			// does the user have a previously started session ?
			if (maxSession) {
				// try to retrieve the last timestamp 
				tryToRetrieveTimeStamp(userId, userToken, maxSession, callback);
			} else {
				callback( 0, JSON.stringify( new LoginResponse(userToken, -1, -1 )));
			}
		} 
	});
}


function tryToRetrieveTimeStamp(userId, userToken, maxSession, callback) {
	_sessionDb.get("select max(timeStamp) from sessions where userId = ? and session = ?", userId, maxSession,
		(maxTimeStampErr, maxTimeStampRow) => {
			if (maxTimeStampErr) {
				callback( -1, "error while retrieving max timestamp (err=" + maxTimeStampErr + ")." );
			} else {
				var maxTimestamp = maxTimeStampRow["max(timeStamp)"];

				if (maxTimestamp) {
					callback( 0, JSON.stringify( new LoginResponse(userToken, maxSession, maxTimestamp )));
				} else {
					callback( 0, JSON.stringify( new LoginResponse(userToken, maxSession, -1 )));
				}
			}
		}
	);
}

/** Generates a unique randomized token off the user id a*/
function generateToken(id, user, password) {
	var seed = id + "-" + user + "-" + password + "-" + new Date().getMilliseconds();
	var rng = seedrandom(seed);
	var token = "" + id;

	for (var i = 0; i < maxTokenLength; i++) {
		token += Math.floor(rng() * 10);	
	}
	return token;
}

/*
 * Insert the properties in a slot for the given user id.
 */
function insertValues(valuesCollection, callback)
{	
	const sqlCall = "insert into sessions values "  + valuesCollection.join();
	_sessionDb.run(sqlCall, (err) => {
		if (err) {
			callback(err, "error while inserting values.");
		} else {
			callback(0, "ok");
		} 
	});
}


