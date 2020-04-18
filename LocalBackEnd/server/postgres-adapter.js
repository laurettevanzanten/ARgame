const {Client} = require('pg');

var _database;

exports.connect = function(name) {
    _database = new Client({
		user: 'postgres',
		host: '127.0.0.1',
		database: name,
		password: 'admin',
		port: 5432
	});
	_database.connect();
    return exports;
}

/*
 * Login to the back-end using the given user name and password
 */
exports.login = function(userName, password, callback) {

	// login the user
	const sqlStatement = `select userId from users where name = '${userName}' and password = '${password}'`;

	_database.query(sqlStatement, (err, res) => {
		if (err ) {
			callback( -1, "db error (err=" + err+ ")." );
		} else if (!res || res.rows.length === 0) {
			callback( -1, "user or password " + userName + " not found." );
		}  else {	
			callback( 0, res.rows[0].userid);
		}
	});
}

exports.getMaxSession = function(userId, callback) {
    // get the last session the user was working on
    const sqlStatement = `select max(session) from sessions where userId = ${userId}`;

	_database.query(sqlStatement, (err, result) => {
		if (err) {
			callback( -1, "error while retrieving max session (err=" + err + ")." );
		} else { 
			callback(0, result.rows[0].max);
		} 
	});
}


exports.getMaxTimeStamp = function(userId, maxSession, callback) {
    const sqlStatement = `select max(timeStamp) from sessions where userId = ${userId} and session = ${maxSession}`;
    
    _database.query(sqlStatement, (err, result) => {
			if (err) {
				callback( -1, "error while retrieving max timestamp (err=" + err + ")." );
			} else {
				callback( 0, result.rows[0].max);
			}
		}
	);
}

/*
 * Insert the properties in a slot for the given user id.
 */
exports.insertValues = function(valuesCollection, callback) {	
	const sqlCall = `insert into sessions values ${valuesCollection}`;
    
    _database.query(sqlCall, (err) => {
		if (err) {
			callback(-1, "error while inserting values (err="+err +").");
		} else { 
			callback(0, "ok");
		} 
	});
}