
var _ = require('underscore');
var fs = require('fs');
var read = require('read');
var Parse = require('parse').Parse;
Parse.initialize(
    "dRJrkKFywmUEYJx10K96Sw848juYyFF01Zlno6Uf",
    "1zVdPAD53kybPNtY2m5X7uVyEnKBE3zK5b1nWluL");

var eachTime = function*(d) {
  while (true) {
    yield d;
    d = new Date(d.getTime() + 1000 * 60 * 15);
  }
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
  //q.doesNotExist("deleted");
  q.equalTo("deleted", null);
  q.descending("datetime");
  q.limit(1000);
  return q.find();

}).then(function(entries) {
  console.log("Analyzing " + entries.length + " entries...");
  entries = entries.reverse();

  var startDate = entries[0].get("datetime");
  var endDate = entries[entries.length - 1].get("datetime");

  // Fast forward to the next noon.
  var startDateNoon = new Date(startDate);
  startDateNoon.setHours(12, 0, 0);
  if (startDate > startDateNoon) {
    startDateNoon = new Date(startDateNoon.getTime() + 1000 * 60 * 60 * 24);
  }
  startDate = startDateNoon;

  // Rewind to the previous noon.
  var endDateNoon = new Date(endDate);
  endDateNoon.setHours(12, 0, 0);
  if (endDate < endDateNoon) {
    endDateNoon = new Date(endDateNoon.getTime() - 1000 * 60 * 60 * 24);
  }
  endDate = endDateNoon;

  var times = eachTime(startDate);
  var t = times.next().value;
  console.log("Generating map for time range: [" + startDate + ", " + endDate + ").");

  var html = "<html>\n"
  html = html + "<head><style>\n";
  html = html + " table { border-collapse: collapse; border: solid black 1px; width: 100% }\n";
  html = html + "    th { font-size: 8pt; }\n";
  html = html + "    td { font-size: 8pt; width: 1%; height: 5px; border-bottom: solid white 1px; }\n";
  html = html + ".white { background-color: white;   }\n";
  html = html + ".light { background-color: #555555; }\n";
  html = html + " .dark { background-color: #aaaaaa; }\n";
  html = html + ".black { background-color: black;   }\n";
  html = html + "  .top { border-left: solid black 1px; border-right: solid black 1px; border-bottom: solid black 1px; text-align: left; }\n";
  html = html + " .left { border-left: solid black 1px; border-right: solid black 1px; width: 4% }\n";
  html = html + ".right { border-right: solid black 1px; }\n";
  html = html + ".bottom { border-bottom: solid black 1px; }\n";
  html = html + "</style></head>\n";
  html = html + "<body><table><tr>\n";
  html = html + "<th>&nbsp;</th>\n";
  html = html + '<th class="top" colspan="28">Noon</th>\n';
  html = html + '<th class="top" colspan="20">7pm</th>\n';
  html = html + '<th class="top" colspan="28">Midnight</th>\n';
  html = html + '<th class="top" colspan="20">7am</th>\n';

  var i = 0;

  var asleep = null;
  var wasEverAsleep = null;
  var wasAlwaysAsleep = null;
  var count = 0;
  var started = false;

  outer:
  while (true) {
    if (t <= entries[i].get("datetime")) {
      // The next entry is in the future, just propagate the asleep state.
      if (!_.isNull(asleep)) {
        var cssClass = "";
        if (started) {
          if (wasAlwaysAsleep) {
            if (count > 1) {
              process.stdout.write("▒");
              cssClass = "dark";
            } else {
              process.stdout.write("█");
              cssClass = "black";
            }
          } else if (wasEverAsleep) {
            process.stdout.write("░");
            cssClass = "light";
          } else {
            process.stdout.write(" ");
            cssClass = "white";
          }
          if (t.getHours() == 0 && t.getMinutes() == 0) {
            process.stdout.write("║");
            cssClass = cssClass + " right";
          }
          if (t.getHours() == 7 && t.getMinutes() == 0) {
            process.stdout.write("┃");
            cssClass = cssClass + " right";
          }
          if (t.getHours() == 19 && t.getMinutes() == 0) {
            process.stdout.write("┃");
            cssClass = cssClass + " right";
          }
        }
        if (t.getHours() == 12 && t.getMinutes() == 0) {
          cssClass = cssClass + " right";
        }
        if (t.getTime() > (endDate.getTime() - 1000 * 60 * 60 * 24)) {
          cssClass = cssClass + " bottom";
        }
        if (started) {
          html = html + '<td class="' + cssClass + '">&nbsp;</td>\n';
        }
        if (t.getHours() == 12 && t.getMinutes() == 0) {
          if (!started) {
            process.stdout.write("\n\n\n\n");
            started = true;
          }
          if (t.getTime() == endDate.getTime()) {
            started = false;
          } else {
            process.stdout.write("\n        " + t + " ");
            html = html + '</tr><tr><th class="left">' + t.getMonth() + '/' + t.getDate() + '</th>\n';
          }
        }
        wasEverAsleep = asleep;
        wasAlwaysAsleep = asleep;
        count = 1;
      }
      t = times.next().value;
    } else {
      // The next entry is in this time span. Record that there was sleeping
      // and move to the next entry.
      asleep = entries[i].get("asleep");
      wasEverAsleep = wasEverAsleep || asleep;
      wasAlwaysAsleep = wasAlwaysAsleep && asleep;
      count++;
      i = i + 1;
      if (i >= entries.length) {
        break outer;
      }
    }
  }
  process.stdout.write("\n");
  html = html + '</tr></table></body></html>\n';
  return html;

}).then(function(html) {
  var promise = new Parse.Promise();
  fs.open("./table.html", "w", function(err, fd) {
    if (err) {
      promise.reject(err);
    } else {
      promise.resolve({ html: html, fd: fd });
    }
  });
  return promise;

}).then(function(htmlfd) {
  html = htmlfd.html;
  fd = htmlfd.fd;

  var buf = new Buffer(html, 'utf8');

  var promise = new Parse.Promise();
  fs.write(fd, buf, 0, buf.length, null, function(err, written, buffer) {
    if (err) {
      promise.reject(err);
    } else {
      promise.resolve(fd);
    }
  });
  return promise;

}).then(function(fd) {
  fs.close(fd);

}).then(function() {
  console.log("\n\n\n\nSuccess!");
}, function(e) {
  console.log("Error: " + e);
});

