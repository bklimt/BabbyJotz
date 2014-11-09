
var _ = require("underscore");
var util = require("cloud/util");
var Buffer = require('buffer').Buffer;

util.declare("logSyncReport", {
  required: {
    report: _.isString
  }

}, function(request) {
  if (!request.master && !request.user) {
    return Parse.Promise.error("Must be logged in.");
  }

  var buffer = new Buffer(request.params.report, "utf8");
  var file = new Parse.File("report.txt", {
    base64: buffer.toString('base64')
  });

  return file.save().then(function() {
    var obj = new Parse.Object("SyncReport");
    var acl = new Parse.ACL();
    obj.setACL(acl);
    if (request.user) {
      obj.set("user", request.user);
    }
    obj.set("report", file);
    return obj.save();

  }).then(function() {
    return true;
  });
});
