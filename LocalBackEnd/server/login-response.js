
module.exports = class LoginResponse {
	constructor(token, session, timeStamp){
		this.token = token;
		this.session = session;
		this.timeStamp = timeStamp;
	}
}