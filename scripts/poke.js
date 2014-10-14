
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
  var obj = new Parse.Object("LogEntry");
  obj.id = "vi0ncWLs3j";
  obj.set({
    poop: true,
    asleep: true,
    formula: 1000.5,
    text: "Hello World!",
    deleted: new Date(),
    time: new Date()
  });
  return obj.save();

}).then(function() {
  console.log("Success!");
}, function(e) {
  console.log("Error: " + JSON.stringify(e));
});

