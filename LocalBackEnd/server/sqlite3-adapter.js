const sqlite3 = require('sqlite3').verbose();

var _database;

exports.connect = function(name) {
    _database = new sqlite3.Database(name);
    return exports;
}

/*
 * Login to the back-end using the given user name and password
 */
exports.login = function(userName, password, callback) {

	// login the user
	const sqlStatement = `select userId from users where name = '${userName}' and password = '${password}'`;

	_database.get(sqlStatement, (err, row) => {
		if (err ) {
			callback( -1, "db error (err=" + err+ ")." );
		} else if (!row) {
			callback( -1, "user or password " + userName + " not found." );
		}  else {	
			callback( 0, row.userId);
		}
	});
}

exports.getMaxSession = function(userId, callback) {
    // get the last session the user was working on
    const sqlStatement = `select max(session) from sessions where userId = ${userId}`;

	_database.get(sqlStatement, (err, row) => {
		if (err) {
			callback( -1, "error while retrieving max session (err=" + err + ")." );
		} else { 
			// note the syntac assumes a SQLite3 response 
			callback(0, row["max(session)"]);
		} 
	});
}


exports.getMaxTimeStamp = function(userId, maxSession, callback) {
    const sqlStatement = `select max(timeStamp) from sessions where userId = ${userId} and session = ${maxSession}`;
    
    _database.get(sqlStatement, (err, row) => {
			if (err) {
				callback( -1, "error while retrieving max timestamp (err=" + err + ")." );
			} else {
				callback( 0, row["max(timeStamp)"]);
			}
		}
	);
}

/*
 * Insert the properties in a slot for the given user id.
 */
exports.insertValues = function(valuesCollection, callback) {	
	const sqlCall = `insert into sessions values ${valuesCollection}`;
    
    _database.run(sqlCall, (err) => {
		if (err) {
			callback(-1, "error while inserting values (err="+err +").");
		} else { 
			callback(0, "ok");
		} 
	});
}