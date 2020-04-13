const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const seedrandom = require('seedrandom');

const winston = require('winston');
const { MESSAGE } = require("triple-beam");

const logger = winston.createLogger({
  level: 'info',
  format: winston.format.json(),
  transports: [
    new winston.transports.File({ filename: 'laurette-server.log' }),
    new winston.transports.Console({
		//
		// Possible to override the log method of the
		// internal transports of winston@3.0.0.
		//
		log(info, callback) {
			setImmediate(() => this.emit("logged", info));

			console.log(info[MESSAGE]);
		
			if (callback) {
				callback();
			}
		}
	  })
  ]
});

const sqlite3 = require('sqlite3').verbose();

// open the database
var sessionsDb = new sqlite3.Database("sessions.db");

// Create Express app
const app = express();

const jsonParser = bodyParser.json();

const whitleListDomain = ['http://localhost:8080',
							// http://alloweddomain.com' is a placeholder
							'http://alloweddomain.com'];



// token can only live for 5 minutes
const maxTokenAge = 60 * 5;
var _userCredentials = {};

// how often do we flush the read/write quuees 
const flushTimeout = 1000;

var _writeQueue = [];
var _readQueue = [];

var _activeQueue;


class WebOperation {
	constructor(name, request, response) {
		this.name = name;
		this.request = request;
		this.response = response;
	}
}

class UserCredentials {
	constructor(id, token, date) {
		this.id = id;
		this.token = token;
		this.date = date;
	}
}

class LoginResponse {
	constructor(token, session, timeStamp){
		this.token = token;
		this.session = session;
		this.timeStamp = timeStamp;
	}
}

/*
 * setup Cross Domain calls
 */
app.use(cors({
  origin: function(origin, callback){
    // allow requests with no origin 
    // (like mobile apps or curl requests)
    if(!origin) return callback(null, true);
    if(whitleListDomain.indexOf(origin) === -1){
      var msg = 'The CORS policy for this site does not allow access from the specified Origin.';
      return callback(new Error(msg), false);
    }
    return callback(null, true);
  }
}));

/*
 * Sends a default page
 */
app.get('/', (req, res) => res.send('Laurette - Server, thesis v0.001'));

/*
 * Logs the user in based off a user and password (plain text -- we're _really_ not expecting the game to
 * be hacked) 
 */
app.post('/login', jsonParser, (req, res) => {
	logger.info(getSource(req) + " logging in as " + req.body.user + ", " + req.body.password); 
	_readQueue.push( new WebOperation("login",  req,  res ))
});

/*
 * Logs the user out
 */
app.post('/logout', jsonParser, (req, res) => {
	logger.info(getSource(req) + " logging out with " + req.body.token); 

	if (_userCredentials[req.body.token]) {
		delete _userCredentials[req.body.token];
	} 

	res.end();
});

/*
 * Handle a order post.
 */
app.post('/post-order', jsonParser, function (req, res) {

	logger.info(getSource(req) + " sending order with " + req.body.token); 
	
	var credentials  = _userCredentials[req.body.token];
	
	if (req && req.body && credentials) {
		// get the age of the token and see if it is still valid
		var now = new Date();
		var credentialsAge = (now.getTime() - credentials.date.getTime()) / 1000;
		
		if (credentialsAge < maxTokenAge) {
			// update the token's life
			_userCredentials[req.body.token].date = now;
			_writeQueue.push(new WebOperation("insert-order", req, res));

		} else {
			replyWithError(req, res, "token is no longer valid.");
			delete _userCredentials[req.body.token];
		}
	} else {
		replyWithError(req, res, "invalid request or token provided.");
	}
});

function replyWithError(request, response, message) {
	logger.error(getSource(request) + " post order with outdated token, credentials = " + JSON.stringify(credentials));
			
	response.statusMessage = message;
	response.status(400).end();
}

function getSource(request) {
	var originSource = request.headers['x-forwarded-for'];

	if (!originSource) {
		originSource = request.connection.remoteAddress;
	}

	return originSource ? originSource : "unknown";
}

function updateQueues() {

	if (_activeQueue === _readQueue) {
		var temp = _readQueue;
		_readQueue = [];
		flushReadQueue(temp, () => {
			_activeQueue = _writeQueue;
			setTimeout(updateQueues, flushTimeout );
		});
	} else {
		var temp = _writeQueue;
		_writeQueue = [];
		flushWriteQueue(temp, () => {
			_activeQueue = _readQueue;
			setTimeout(updateQueues, flushTimeout );
		});
	}
}

function flushReadQueue(queue, onCompleteCallback) {

	if (queue.length > 0) {
		var outstandingOperations = queue.length;

		for (var i = 0; i < queue.length; i++) {
			const request = queue[i].request;
			const response = queue[i].response;
			
			login(request.body.user, request.body.password, (errCode, message) => {
				
				if (errCode == 0) {
					response.send(message);
				} else {
					replyWithError(req, res, message);
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

function flushWriteQueue(queue, onCompleteCallback) {
	if (queue.length == 0) {
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
					sendReply(res, req.body.timeStamp, err,  "Err: " + err);
				} else {
					sendReply(res, req.body.timeStamp, 0, "Ok");
				}

				outstandingOperations--;

				if (outstandingOperations == 0) {
					onCompleteCallback();
				}
			}
		});
	}
}

/*
 * Login to the back-end using the given user name and password
 */
function login(name, password, callback) {

	// login the user
	sessionsDb.get("select userId from users where name = ? and password = ?", name, password, (err, row) => {
		if (err ) {
			callback( -1, "db error (err=" +err+ ")." );
		} else if (!row) {
			callback( -1, "user or password " + name + " not found." );
		}  else {	
			const id = 	row.userId;	
			const token = generateToken(id, name, password);

			_userCredentials[token] = new UserCredentials(id, token, new Date());

			// get the last session the user was working on
			sessionsDb.get("select max(session) from sessions where userId = ?", id, (maxSessionErr, maxSessionRow) => {
				if (maxSessionErr) {
					callback( -1, "error while retrieving max session (err=" + maxSessionErr + ")." );
				} else { 
					var maxSession = maxSessionRow["max(session)"];

					if (maxSession) {
						sessionsDb.get("select max(timeStamp) from sessions where userId = ? and session = ?", id, maxSession,
							(maxTimeStampErr, maxTimeStampRow) => {
								if (maxTimeStampErr) {
									callback( -1, "error while retrieving max timestamp (err=" + maxTimeStampErr + ")." );
								} else {
									var maxTimestamp = maxTimeStampRow["max(timeStamp)"];

									if (maxTimestamp) {
										callback( 0, JSON.stringify( new LoginResponse(token, maxSession, maxTimestamp )));
									} else {
										callback( 0, JSON.stringify( new LoginResponse(token, maxSession, -1 )));
									}
								}
							});
						
					} else {
						callback( 0, JSON.stringify( new LoginResponse(token, -1, -1 )));
					}
				} 
			});
		}
	});
}


function generateToken(id, user, password) {
	var seed = id + "-" + user + "-" + password + "-" + new Date().getMilliseconds();
	var rng = seedrandom(seed);
	var token = "";

	for (var i = 0; i < 8; i++) {
		token += Math.floor(rng() * 10);	
	}
	return token;
}

/*
 * Create and send a reply to the client
 */ 
function sendReply(response, timeStamp, errorCode, message)
{
	var obj = { errorCode: errorCode, timeStamp: timeStamp, message : message };
	response.send(JSON.stringify(obj));
}

/*
 * Insert the properties in a slot for the given user id.
 */
function insertValues(valuesCollection, callback)
{	
	const sqlCall = "insert into sessions values "  + valuesCollection.join();
	sessionsDb.run(sqlCall, (err) => {
		if (err) {
			callback(err, "error while inserting values.");
		} else {
			callback(0, "ok");
		} 
	});
}

// Start the Express server
app.listen(3000, () => console.log('Server running on port 3000!'));

// start flushing the read and write queues
_activeQueue = _readQueue;
setTimeout(updateQueues, 2000);