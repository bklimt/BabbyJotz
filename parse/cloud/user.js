
var _ = require("underscore");

var util = require("cloud/util");

util.declare("login", {
  required: {
    username: _.isString,
    password: _.isString
  }

}, function(request) {
  return Parse.Promise.as().then(function() {
    return Parse.User.logIn(request.params.username, request.params.password);

  }).then(function(user) {
    return {
      objectId: user.id,
      sessionToken: user.getSessionToken()
    };
  });
});

util.declare("signup", {
  required: {
    username: _.isString,
    password: _.isString,
    email: _.isString
  }

}, function(request) {
  return Parse.Promise.as().then(function() {
    var user = new Parse.User();
    user.set("username", request.params.username);
    user.set("email", request.params.email);
    user.set("password", request.params.password);
    return user.signUp();

  }).then(function(user) {
    return {
      objectId: user.id,
      sessionToken: user.getSessionToken()
    };
  });
});

Parse.Cloud.beforeSave(Parse.User, function(request, response) {
  if (!request.master) {
    if (request.object.id) {
      // It's an existing user.
      if (!request.user) {
        response.error("Must be logged in to update a user.");
        return;
      }
      if (request.user.id !== request.object.id) {
        response.error("Users can only update themselves.");
        return;
      }
    }
  }

  var user = request.object;
  user.setACL(new Parse.ACL());
  response.success();
});

