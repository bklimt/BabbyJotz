
var _ = require("underscore");

Parse.Cloud.beforeSave("Photo", function(request, response) {
  if (!request.master && !request.user) {
    response.error("Must be logged in.");
    return;
  }

  var photo = request.object;
  var babyUuid = photo.get("babyUuid");
  var roleName = "Baby-" + babyUuid;

  Parse.Promise.as().then(function() {
    // Make sure that the photo is for a baby this user can write.
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
        "This user does not have permission to make photos for this baby.");
    }

    var acl = new Parse.ACL();
    acl.setRoleReadAccess(roleName, true);
    acl.setRoleWriteAccess(roleName, true);
    photo.setACL(acl);

    // Make deleted be consistent.
    if (photo.get("deleted") === null) {
      photo.unset("deleted");
    }

  }).then(function() {
    response.success();
  }, function(error) {
    response.error(error);
  });
});

