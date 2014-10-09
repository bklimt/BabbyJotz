
var _ = require('underscore');
var read = require('read');
var Parse = require('parse').Parse;

Parse.initialize(
    "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
    "1zVdPAD53kybPNtY2m5X7uVyEnKBE3zK5b1nWluL");

var newUTC = function(year, month, day, hour, minute) {
  var d = new Date(0);
  d.setUTCFullYear(year, month - 1, day);
  d.setUTCHours(hour, minute);
  return d;
};

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
  q.descending("time");
  q.doesNotExist("deleted");
  q.limit(1000);
  return q.find();

}).then(function(entries) {
  console.log("analyzing " + entries.length + " entries...");
  entries = entries.reverse();
  var startOfNap = null;
  var napTotal = 0;

  var startNap = function(d) {
    if (!startOfNap) {
      startOfNap = d;
    }
  };

  var endNap = function(d) {
    if (!startOfNap) {
      return false;
    }

    var endOfNap = d;
    var month = endOfNap.getUTCMonth() + 1;
    var day = endOfNap.getUTCDate();
    var startHour = startOfNap.getUTCHours();
    var startMinute = startOfNap.getUTCMinutes();
    var endHour = endOfNap.getUTCHours();
    var endMinute = endOfNap.getUTCMinutes();
    var duration = Math.round(((endOfNap - startOfNap) / 1000.0) / 60.0);
    napTotal = napTotal + duration;

    console.log(month + "/" + day + ": " + duration + " minutes from " + startHour + ":" + startMinute + " to " + endHour + ":" + endMinute);

    startOfNap = null;
    return true;
  };

  var endDay = function(d) {
    var currentDay = d.getUTCDate();
    var month = d.getUTCMonth() + 1;
    var midnight = newUTC(2014, month, currentDay, 0, 0);
    var beforeMidnight = new Date(midnight - 1);
    if (endNap(beforeMidnight)) {
      startNap(midnight);
    }

    var beforeMonth = beforeMidnight.getUTCMonth() + 1;
    var beforeDay = beforeMidnight.getUTCDate();
    console.log(beforeMonth + "/" + beforeDay + "\t" + napTotal);
    napTotal = 0;
  };

  var previousDay = null;
  _.each(entries, function(entry) {
    var currentDay = entry.get("time").getUTCDate();
    if (previousDay && (currentDay !== previousDay)) {
      endDay(entry.get("time"));
    }
    previousDay = currentDay;

    if (entry.get("asleep")) {
      startNap(entry.get("time"));
    } else {
      endNap(entry.get("time"));
    }
  });

  endNap(entries[entries.length - 1].get("time"));

}).then(function() {
  console.log("Success!");
}, function(e) {
  console.log("Error: " + e);
});

