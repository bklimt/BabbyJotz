
var _ = require("underscore");

Parse.Cloud.beforeSave("Baby", function(request, response) {
  var baby = request.object;
  var uuid = baby.get("uuid");
  var user = request.user;

  if (!request.master && !user) {
    response.error("Must be logged in to save a baby.");
    return;
  }

  if (!request.master && !uuid) {
    response.error("Baby is missing UUID.");
    return;
  }

  if (!request.master && !baby.get("name")) {
    response.error("Baby is missing name.");
  }

  // Normalize the "deleted" field.
  if (baby.get("deleted") === null) {
    baby.unset("deleted");
  }

  // The baby should only ever be accessible from its special role.
  var roleName = "Baby-" + baby.get("uuid");
  var acl = new Parse.ACL();
  acl.setRoleReadAccess(roleName, true);
  acl.setRoleWriteAccess(roleName, true);
  baby.setACL(acl);

  if (baby.id) {
    // It's not new, so no need to create the role.
    response.success();
    return;
  }

  var role = new Parse.Role(roleName, acl);
  Parse.Promise.as().then(function() {
    // Add the creator of the baby to the role for the baby.
    if (user) {
      var users = role.getUsers();
      users.add(user);
    }
    return role.save();

  }).then(function() {
    response.success();
  }, function(error) {
    response.error(error);
  });
});

