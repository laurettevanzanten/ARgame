#Warehouse Game Architecture 

##Prerequisites:

* Install Unity 3d from https://unity3d.com/get-unity/download
* Install Node Package Manager (NPM) from https://www.npmjs.com/get-npm
* Install Http-Server Globally via NPM (See https://www.npmjs.com/package/http-server), ie run "npm install http-server -g" from the command line
* Install Node-JS from https://nodejs.org/en/download/
* Install SQLite from https://sqlite.org/download.html. Download the windows Binaries and put them in the LocalBackend folder.
* Install all packages in LocalBackend (in LocalBackend on the command line type 'npm install')
* Install Visual Code to debug the back-end server (https://code.visualstudio.com/Download)


##Client

###Getting started:

Note the following assumes some knowledge of Unity and how to build local builds and web builds.

The client is implemented in Unity and will be deployed on the localhost or via GithubPages. The majority of the client can be found in the Assets folder. Unity can build to a web client (note this will take some time, up to several minutes on a pretty beefy PC). The web client, which takes the form of an index.html page, cannot be run stand alone like the Windows binaries and _needs_ to be hosted via a webserver. There are many different ways to running a webserver, for testing and development it's recommended to run npm's http-server. 

In the folder containing the Unity Web Client (index.html) run from the command line 'http-server'. Now open a browser with the address "http://localhost:8080" and you should see the unity application in your webbrowser. 

##Server

For the webserver we use node.js with the Express packages. The webserver (in the LocalBackend) is a very simple javascript file which runs a (very) naive and simplistic "user authentication" scheme and allows for inserting completed orders into a local database.

To run the node server, run "node server.js" in the LocalBackend directory or debug via Visual Code.

##Database
The database is implemented by running SQLite on the back-end. 

Commands to run against SQLite from the commandline (assuming the db is called sessions.db):

'sqlite3 sessions.db < createdb.sql3', runs the createdb script and creates a new db
'sqlite sessions.db "insert into sessions values(0, 1, 9, 1, 9);"'

In the folder LocalBackEnd\SqlScripts a number of scripts can be found to make life easier. 
