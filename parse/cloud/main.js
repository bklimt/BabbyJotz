
var _ = require("underscore");
var moment = require("moment");

var desc = function(entry) {
  if (entry.get("deleted")) {
    return "(deleted)";
  }

  return (
      moment(entry.get("time")).format("hh:mm a") + " - " +
      (entry.get("asleep") ? "\ud83d\udca4 " : "") +
      (entry.get("poop") ? "\ud83d\udca9 " : "") +
      (entry.get("formula") > 0
          ? ("\ud83c\udf7c " + entry.get("formula") + "oz ")
          : "") +
      entry.get("text"));
};

Parse.Cloud.beforeSave("LogEntry", function(request, response) {
  if (!Parse.User.current()) {
    response.error("Must be logged in.");
    return;
  }

  // Request an ACL for the current user.
  var acl = new Parse.ACL(Parse.User.current());
  request.object.setACL(acl);

  // Make deleted be consistent.
  if (request.object.get("deleted") == null) {
    request.object.unset("deleted");
  }

  // Old clients set this old field with the wrong timezone.
  if (!request.object.has("datetime")) {
    request.object.set("datetime",
        new Date(Date.parse(
            request.object.get("time").toUTCString().replace(
                " GMT", " PDT"))));
  }

  // Send a push.
  var userId = Parse.User.current().id;
  var text = desc(request.object);
  console.log("Sending push to " + userId + ": \"" + text + "\"");

  var query = new Parse.Query(Parse.Installation);
  query.equalTo("userId", userId);

  var data = {};
  if (!request.object.get("deleted")) {
    data = { alert: text };
  }

  Parse.Promise.as().then(function() {
    return Parse.Push.send({
      where: query,
      data: data
    });

  }).then(function() {
    response.success();
  }, function(error) {
    response.error(JSON.stringify(error));
  });
});
