
var _ = require("underscore");
var util = require("cloud/util");
var Buffer = require('buffer').Buffer;

util.declare("logEvent", {
  required: {
    name: _.isString,
    instance: _.isString,
    platform: _.isString
  }

}, function(request) {
  var obj = new Parse.Object("Event");
  var acl = new Parse.ACL();
  obj.setACL(acl);
  if (request.user) {
    obj.set("user", request.user);
  }
  obj.set(request.params);
  return obj.save().then(function() {
    var dim = _.omit(request.params, 'name', 'instance');
    return Parse.Analytics.track(request.params.name, dim);

  }).then(function() {
    return true;
  });
});

util.declare("logSyncReport", {
  required: {
    report: _.isString
  }

}, function(request) {
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
    obj.set("platform", request.params.platform);
    obj.set("instance", request.params.instance);
    return obj.save();

  }).then(function() {
    var dim = _.omit(request.params, 'report');
    return Parse.Analytics.track('syncReport', dim);

  }).then(function() {
    return true;
  });
});

util.declare("logException", {
  required: {
    exception: _.isString
  }

}, function(request) {
  var buffer = new Buffer(request.params.exception, "utf8");
  var file = new Parse.File("exception.txt", {
    base64: buffer.toString('base64')
  });

  return file.save().then(function() {
    var obj = new Parse.Object("Exception");
    var acl = new Parse.ACL();
    obj.setACL(acl);
    if (request.user) {
      obj.set("user", request.user);
    }
    obj.set("exception", file);
    obj.set("platform", request.params.platform);
    obj.set("instance", request.params.instance);
    return obj.save();

  }).then(function() {
    return Parse.Analytics.track('exception', {
      platform: request.params.platform
    });

  }).then(function() {
    return true;
  });
});
