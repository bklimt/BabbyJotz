
var _ = require('underscore');
var read = require('read');
var Parse = require('parse').Parse;
Parse.initialize(
    "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
    "1zVdPAD53kybPNtY2m5X7uVyEnKBE3zK5b1nWluL");


Parse.Promise.as().then(function() {
  var promise = new Parse.Promise();
  read({ prompt: 'Username: ' }, function(error, username) {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(username);
    }
  });
  return promise;

}).then(function(username) {
  var promise = new Parse.Promise();
  read({ prompt: 'Password: ', silent: true }, function(error, password) {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve({ username: username, password: password });
    }
  });
  return promise;

}).then(function(userpass) {
  return Parse.User.logIn(userpass.username, userpass.password);

}).then(function() {
  var q = new Parse.Query("LogEntry");
  //q.doesNotExist("datetime");
  q.doesNotExist("deleted");
  q.limit(1000);
  return q.find();

}).then(function(entries) {
  console.log("fixing " + entries.length + " dates...");
  var p = Parse.Promise.as();
  _.each(entries, function(obj) {
    p = p.then(function() {
      // obj.set("datetime",
      //     new Date(Date.parse(
      //         obj.get("time").toUTCString().replace(" GMT", " PDT"))));
      obj.set("deleted", null);
      return obj.save();
    });
  });
  return p;

}).then(function() {
  console.log("Success!");
}, function(e) {
  console.log("Error: " + e);
});

