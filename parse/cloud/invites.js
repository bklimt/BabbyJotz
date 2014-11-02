
var _ = require("underscore");
var util = require("cloud/util");

util.declare("invite", {
  required: {
    babyUuid: _.isString,
    username: _.isString
  }

}, function(request) {
  if (!request.master && !request.user) {
    return Parse.Promise.error("Must be logged in.");
  }

  // Make sure the user is allowed to write to this baby.
  var roleName = "Baby-" + request.params.babyUuid;
  var roleQuery = new Parse.Query(Parse.Role);
  roleQuery.equalTo("name", roleName);
  
  return roleQuery.first().then(function(role) {
    if (!role) {
      return Parse.Promise.error("Baby doesn't exist.");
    }

    // If we managed to read it, then the user has access.
    var invite = new Parse.Object("Invite");
    invite.set("username", request.params.username);
    invite.set("babyUuid", request.params.babyUuid);
    invite.setACL(new Parse.ACL());
    return invite.save();
  }).then(function() {
    return true;
  });
});


util.declare("acceptInvite", {
  required: {
    inviteId: _.isString
  }

}, function(request) {
  if (!request.user) {
    return Parse.Promise.error("Only users can accept invites.");
  }

  // Make sure the current user matches the person on the invite.
  return Parse.Promise.as().then(function() {
    var inviteQuery = new Parse.Query("Invite");
    return inviteQuery.get(request.params.inviteId, { useMasterKey: true });

  }).then(function(invite) {
    if (request.user &&
        request.user.get("username") !== invite.get("username")) {
      return Parse.Promise.error(
          "Users can only accept their own invites.");
    }

    // Everything is cool.
    var babyUuid = invite.get("babyUuid");
    var roleName = "Baby-" + babyUuid;
    var roleQuery = new Parse.Query(Parse.Role);
    roleQuery.equalTo("name", roleName);
    return roleQuery.first({ useMasterKey: true }).then(function(role) {
      if (!role) {
        return Parse.Promise.error("Baby does not exist.");
      }

      role.getUsers().add(request.user);
      return role.save(null, { useMasterKey: true });

    }).then(function() {
      return invite.destroy({ useMasterKey: true });

    }).then(function() {
      return true;

    });
  });
});


util.declare("listInvites", {}, function(request) {
  if (!request.user) {
    return Parse.Promise.error("Only users can list invites.");
  }

  var query = new Parse.Query("Invite");
  query.equalTo("username", request.user.get("username"));
  query.limit(10);
  query.descending("updatedAt");
  return query.find({ useMasterKey: true }).then(function(invites) {
    var uuids = _.map(invites, function(invite) {
      return invite.get("babyUuid");
    });

    var babyQuery = new Parse.Query("Baby");
    babyQuery.containedIn("uuid", uuids);
    return babyQuery.find({ useMasterKey: true }).then(function(babies) {
      var results = [];
      _.each(invites, function(invite) {
        _.each(babies, function(baby) {
          if (baby.get("uuid") === invite.get("babyUuid")) {
            results.push({
              inviteId: invite.id,
              babyUuid: baby.get("uuid"),
              babyName: baby.get("name")
            });
          }
        });
      });

      var myBabiesQuery = new Parse.Query("Baby");
      return myBabiesQuery.find().then(function(myBabies) {
        _.each(myBabies, function(baby) {
          results.push({
            inviteId: null,
            babyUuid: baby.get("uuid"),
            babyName: baby.get("name")
          });
        });
        return { babies: results };
      });
    });
  });
});

util.declare("unlink", {
  required: {
    babyUuid: _.isString
  }

}, function(request) {
  if (!request.user) {
    return Parse.Promise.error("Only users can unlink babies.");
  }

  // Make sure the user is allowed to write to this baby.
  var roleName = "Baby-" + request.params.babyUuid;
  var roleQuery = new Parse.Query(Parse.Role);
  roleQuery.equalTo("name", roleName);
  
  return roleQuery.first().then(function(role) {
    if (!role) {
      return Parse.Promise.error("Baby doesn't exist.");
    }

    role.getUsers().remove(request.user);
    return role.save();

  }).then(function(role) {
    // TODO: Don't let the last person unlink.
    return true;

  });
});

