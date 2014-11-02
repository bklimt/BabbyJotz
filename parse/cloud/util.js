
var _ = require("underscore");

// Declares a new API endpoint.
exports.declare = function(name, options, func) {
  Parse.Cloud.define(name, function(request, response) {
    Parse.Promise.as().then(function() {
      if (options.required) {
        var error = null;
        _.each(options.required, function(validate, key) {
          if (!_.has(request.params, key)) {
            error = "Missing parameter: \"" + key + "\".";
          } else {
            if (!validate(request.params[key])) {
              error = "Incorrect type: \"" + key + "\".";
            }
          }
        });
        if (error) {
          return Parse.Promise.error(error);
        }
      }
      return func(request);

    }).then(function(result) {
      response.success(result);
    }, function(error) {
      response.error(error);
    });
  });
};

