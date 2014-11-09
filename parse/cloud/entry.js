
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
  if (!request.master && !request.user) {
    response.error("Must be logged in.");
    return;
  }

  var entry = request.object;
  var babyUuid = entry.get("babyUuid");
  var roleName = "Baby-" + babyUuid;

  Parse.Promise.as().then(function() {
    // Make sure that the entry is for a baby this user can write.
    // Otherwise, people can write entries in other people's logs.
    // This may be overkill, as guessing a baby's UUID is tantamount to
    // (or even harder than) guessing their password. But it also makes sure
    // the baby actually exists.
    var roleQuery = new Parse.Query(Parse.Role);
    roleQuery.equalTo("name", roleName);
    return roleQuery.first({ useMasterKey: request.master });

  }).then(function(role) {
    if (!role) {
      return Parse.Promise.error(
        "This user does not have permission to make entries for this baby.");
    }

    var acl = new Parse.ACL();
    acl.setRoleReadAccess(roleName, true);
    acl.setRoleWriteAccess(roleName, true);
    entry.setACL(acl);

    // Make deleted be consistent.
    if (entry.get("deleted") === null) {
      entry.unset("deleted");
    }

    // Old clients set this old field with the wrong timezone.
    if (!entry.has("datetime")) {
      entry.set("datetime",
          new Date(Date.parse(
              entry.get("time").toUTCString().replace(
                  " GMT", " PDT"))));
    }

    // Old clients don't even know about these fields.
    if (!entry.has("leftBreast")) {
      entry.set("leftBreast", 0);
    }
    if (!entry.has("rightBreast")) {
      entry.set("rightBreast", 0);
    }
    if (!entry.has("pumped")) {
      entry.set("pumped", 0);
    }

  }).then(function() {
    response.success();
  }, function(error) {
    response.error(error);
  });
});

Parse.Cloud.afterSave("LogEntry", function(request) {
  // Send a push.
  var entry = request.object;
  var babyUuid = entry.get("babyUuid");
  var roleName = "Baby-" + babyUuid;
  var text = desc(entry);

  Parse.Promise.as().then(function() {
    // Get the role for this baby.
    var roleQuery = new Parse.Query(Parse.Role);
    roleQuery.equalTo("name", roleName);
    return roleQuery.first({ useMasterKey: request.master });

  }).then(function(role) {
    // This was already checked in the beforeSave, so it's just a sanity check.
    if (!role) {
      return Parse.Promise.error("Role was not valid in afterSave.");
    }
    return role.getUsers().query().find({ useMasterKey: true });

  }).then(function(users) {
    var promise = Parse.Promise.as();
    var userIds = _.map(users, function(user) { return user.id });
    console.log("Sending push for " + entry.id +
        " to [" + userIds.join(", ") + "]: \"" + text + "\"");

    var query = new Parse.Query(Parse.Installation);
    query.containedIn("userId", userIds);
    // TODO: If we passed installationId in here, we could avoid pinging the
    // device that just did this save.

    var data = {};
    if (!entry.get("deleted")) {
      data = {
        alert: text,
        objectId: entry.id
      };
    }

    return Parse.Push.send({
      where: query,
      data: data
    });

  }).then(function() {
    // Success!
  }, function(error) {
    console.error(JSON.stringify(error));
  });
});
