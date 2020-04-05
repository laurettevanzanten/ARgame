const express = require('express');
const bodyParser = require('body-parser');
const cors = require('cors');
const exec = require('child_process').exec;
const fs = require('fs');

// Create Express app
const app = express();

const sqlExec = "sqlite3.exe sessions.db ";

const jsonParser = bodyParser.json();

const whitleListDomain = ['http://localhost:8080',
							// http://alloweddomain.com' is aplaceholder
							'http://alloweddomain.com'];

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

	const hasBody =  req && req.body;
	const bodyJson = hasBody ? JSON.stringify(req.body) : "undefined";
	console.log("post-session-db " + bodyJson);

	if (hasBody) {
		getUserId(req.body.user, req.body.password, (id) => {
			if (id >= 0)
			{
				var itemList = 	JSON.stringify(req.body.items);
				insert(id, req.body.sessionId, req.body.timeStamp, itemList, (err, stdOut, stdErr) =>
				{
					if (err)
					{
						sendReply(res, -2, "Err: " + err);
					}
					else if (stdErr)
					{
						sendReply(res, -3, "StdErr: " + stdErr);
					}
					else 
					{
						sendReply(res, 0, req.body.timeStamp + ", ok");
					}
				});
			}
			else
			{
				sendReply(res, -1, "user or password not found");
			}
		});
	}	
});


/*
 * Create and send a reply to the client
 */ 
function sendReply(response, errorCode, message)
{
	var obj = { errorCode: errorCode, message : message };
	response.send(JSON.stringify(obj));
}

/*
 * Insert the jsonText in a slot for the given user id.
 */
function insert(userId, session, timeStamp, jsonText, callback)
{
	var sqlCall = sqlExec
		+ '"insert into sessions values( ' 
			+ userId + ', ' 
			+ session + ', '
			+ timeStamp + ', '
			+ '\'' + jsonText + '\''
		+ ' );"';
	
	console.log(sqlCall);
	
	exec(sqlCall, (err, stdOut, stdErr) =>{
		
		if (callback) 
		{
			callback(err, stdOut, stdErr);
		}
		
		if (err)
		{
			console.log(err);
		}

		if (stdErr)
		{
			console.log(stdErr);
		}
	} );
}

/*
 * Returns the user id based on name and password. 
 */
function getUserId(name, password, callback)
{
	var sqlCall = sqlExec 
		+ '"select userId from users where name=\'' + name + '\' '
		+ 'and password=\'' + password + '\';"';
	
	console.log(sqlCall);
	
	exec(sqlCall, (err, stdOut, stdErr) =>{
		callback(!stdOut  ? -1 : parseInt(stdOut));
	} );
}

// Start the Express server
app.listen(3000, () => console.log('Server running on port 3000!'));
