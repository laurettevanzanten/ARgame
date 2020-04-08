const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const exec = require('child_process').exec;
const spawn = require('child_process').spawn;
const fs = require('fs');

// Create Express app
const app = express();

const sqlExec = "sqlite3.exe sessions.db ";

const jsonParser = bodyParser.json();

const whitleListDomain = ['http://localhost:8080',
							// http://alloweddomain.com' is aplaceholder
							'http://alloweddomain.com'];


const flushTimeout = 1000;

var _requestQueue = [];
var _insertQueue = {};
var _userTable = {};
var _uniqueUserRequests = 0;
var _isFlushRequestQueueScheduled = false;

// setup Cross Domain calls
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


// Shows a page when browsing to index.html
app.get('/', (req, res) => res.send('Laurette - Server, thesis v0.001'));

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

	if (req && req.body) {
		_requestQueue.push({index: _requestQueue.length, request: req, response: res});

		if ( !_isFlushRequestQueueScheduled) {
			_isFlushRequestQueueScheduled = true;
			setTimeout(flushRequestQueue, flushTimeout);
		}		
	}
});

function flushRequestQueue()
{
	_userTable = {};
	_insertQueue = {};

	// combine all messages from a single user
	for (var i = 0; i < _requestQueue.length; i++) {
		var key = _requestQueue[i].request.body.user + "-" + _requestQueue[i].request.body.password;
		
		if (!_userTable.hasOwnProperty(key)) {
			_userTable[key] = [];
			_uniqueUserRequests++;
		}
		
		_userTable[key].push(_requestQueue[i]);
	}

	for (var name in _userTable) {
		if (_userTable.hasOwnProperty(name)) {
			
			var msgList = _userTable[name]; 
			var msg = msgList[0];

			getUserId(msg.request.body.user,msg.request.body.password, (id, userName) => {
				
				if (id >= 0) {
					_insertQueue[id] = _userTable[userName];
				}
				else {
					sendReply(msg.response, -1, "user or password not found");
				}

				_uniqueUserRequests--;

				if (_uniqueUserRequests == 0) {
					if (Object.keys(_insertQueue).length > 0) {					
						flushInsertQueue();
					}
					else {
						_isFlushRequestQueueScheduled = false;
					} 

				}
			});
		}
	}

	_requestQueue = [];
}


function flushInsertQueue() {
	var valuesCollection = [];
		
	for (var id in _insertQueue) {
		if (_insertQueue.hasOwnProperty(id)) {
			var msgList = _insertQueue[id];

			for (var i = 0; i < msgList.length; i++) {
				var msg = msgList[i].request.body;
				var itemList = JSON.stringify(msg.items);
				valuesCollection.push("(" + id + "," + msg.sessionId + "," + msg.timeStamp + ",'" + itemList + "')");
			}
		}
	}
	
	var callbackCount = valuesCollection.length;

	insertValues(valuesCollection, (exitCode, err) =>
	{		
		for (var id in _insertQueue) {
			if (_insertQueue.hasOwnProperty(id)) {
				var msgList = _insertQueue[id];

				for (var i = 0; i < msgList.length; i++) {
					var res = msgList[i].response;
					var req = msgList[i].request;
					if (err)
					{
						sendReply(res, req.body.timeStamp, err,  "Err: " + err);
					}
					else 
					{
						sendReply(res, req.body.timeStamp, 0,  "Ok");
					}

					callbackCount--;
					if (callbackCount == 0) {
						_isFlushRequestQueueScheduled = false;
					}
				}
			}
		}
	});
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
		console.log("inserted values, sqlite3 exited with " + exitCode);
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

// Start the Express server
app.listen(3000, () => console.log('Server running on port 3000!'));
