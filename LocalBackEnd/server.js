const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const exec = require('child_process').exec;
const spawn = require('child_process').spawn;
const fs = require('fs');
const seedrandom = require('seedrandom');


// Create Express app
const app = express();

const sqlExec = "sqlite3.exe sessions.db ";

const jsonParser = bodyParser.json();

const whitleListDomain = ['http://localhost:8080',
							// http://alloweddomain.com' is aplaceholder
							'http://alloweddomain.com'];


const flushTimeout = 1000;
// token can only live for 5 minutes
const maxTokenAge = 60 * 5;

// error which occurs when the client didn't provide a request body or invalid token
const invalidBodyOrToken = -5;
// error which occurs if the user is inactive for more than maxTokenAge 
const accessTimeOutError = -6;

var _writeQueue = [];
var _readQueue = [];
var _activeQueue;

var _userCredentials = {};

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
	_readQueue.push( new WebOperation("login",  req,  res ))
});

/*
 * Logs the user out
 */
app.post('/logout', jsonParser, (req, res) => {
	if (_userCredentials[req.body.token]) {
		delete _userCredentials[req.body.token];
	} 

	res.end();
});


// deprecated -- keeping it here in case we do need to move to a file based apporach 
app.post('/post-session-fs', jsonParser, function (req, res) {
	filePath = __dirname + "/" + req.body.user + '.txt';
	fs.appendFile(filePath, JSON.stringify(req.body), function() {
		res.end();
	});
});

/*
 * Handle a post on the database
 */
app.post('/post-session-db', jsonParser, function (req, res) {

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
			sendReply(res, -1, accessTimeOutError, "token is no longer valid.");	
			delete _userCredentials[req.body.token];
		}
	} else {
		sendReply(res, -1, invalidBodyOrToken, "invalid request or token provided.");
	}

});

function updateQueues() {

	if (_activeQueue === _readQueue) {
		var temp = _readQueue;
		_readQueue = [];
		flushReadQueue(temp, () => {
			_activeQueue = _writeQueue;
			setTimeout(updateQueues, 1000 );
		});
	} else {
		var temp = _writeQueue;
		_writeQueue = [];
		flushWriteQueue(temp, () => {
			_activeQueue = _readQueue;
			setTimeout(updateQueues, 1000 );
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
					sendReply(response, 0, errCode, message);
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

		insertValues(valuesCollection, (exitCode, err) => {		
			for (var i = 0; i < queue.length; i++) {
				var msg = queue[i];

				var res = msg.response;
				var req = msg.request;
				
				if (err) {
					sendReply(res, req.body.timeStamp, err,  "Err: " + err);
				} else {
					sendReply(res, req.body.timeStamp, 0,  "Ok");
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
function login(user, password, callback) {

	getUserId(user, password, (id, userName) => {
		if (id === -1) {
			callback( -1, "user or password not found");
		} else  {
			const token = generateToken(id, user, password);

			_userCredentials[token] = new UserCredentials(id, token, new Date());

			getMaxSession(id, (maxSession) => {
				if (maxSession >= 0) {
					getMaxTimeStamp(id, maxSession, (maxTimeStamp) => {
						if (maxTimeStamp >= 0) {
							callback( 0, JSON.stringify( new LoginResponse(token, maxSession, maxTimeStamp )));
						} else {
							callback( 0, JSON.stringify( new LoginResponse(token, -1, -1 )));
						}		
					});
				} else {
					callback( 0, JSON.stringify( new LoginResponse(token, -1, -1 )) );
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
	var child = spawn("sqlite3.exe", ["sessions.db"]);

	child.on('exit', (exitCode) => {
		if (callback) 
		{
			callback(exitCode, null);
		}		
	});

	child.on('error', (exitCode) => {
		if (callback) 
		{
			callback(exitCode, exitCode);
		}
	});

	var sqlInsert = 'insert into sessions values '  + valuesCollection.join() + ';\n'

	child.stdin.setEncoding('utf-8');
	child.stdout.pipe(process.stdout);

	// note - need to write via stdin, simply adding inserts on the command line kills the node process as the command line
	// may become too big.	
	child.stdin.write(sqlInsert);
	child.stdin.write(".exit\n");
	child.stdin.end();
}

/*
 * Returns the user id based on name and password. 
 */
function getUserId(name, password, callback)
{
	var sqlCall = sqlExec 
		+ '"select userId from users where name=\'' + name + '\' '
		+ 'and password=\'' + password + '\';"';
	
	exec(sqlCall, (err, stdOut, stdErr) =>{
		callback(!stdOut  ? -1 : parseInt(stdOut), name  + "-" + password);
	} );
}

function getMaxSession(userId, callback)  {
	var sqlCall = sqlExec 
		+ '"select max(session) from sessions where userId = ' + userId + ';';

	exec(sqlCall, (err, stdOut, stdErr) =>{
		callback(!stdOut  ? -1 : parseInt(stdOut));
	} );
}

function getMaxTimeStamp(userId, sessionId, callback)  {
	var sqlCall = sqlExec 
		+ '"select max(timeStamp) from sessions where userId= ' + userId + ' and session=' + sessionId + ';';

	exec(sqlCall, (err, stdOut, stdErr) =>{
		callback(!stdOut  ? -1 : parseInt(stdOut));
	} );
}

// Start the Express server
app.listen(3000, () => console.log('Server running on port 3000!'));

// start flushing the read and write queues
_activeQueue = _readQueue;
setTimeout(updateQueues, 2000);