
var _ = require("underscore");
var Image = require("parse-image");

var resizePhotoAsync = function(photo) {
  return Parse.Cloud.httpRequest({
    url: photo.get("file").url()
 
  }).then(function(response) {
    var image = new Image();
    return image.setData(response.buffer);
 
  }).then(function(image) {
    // Crop the image to the smaller of width or height.
    var size = Math.min(image.width(), image.height());
    return image.crop({
      left: (image.width() - size) / 2,
      top: (image.height() - size) / 2,
      width: size,
      height: size
    });
 
  }).then(function(image) {
    // Resize the image to 200x200.
    return image.scale({
      width: 200,
      height: 200
    });
 
  }).then(function(image) {
    // Make sure it's a JPEG to save disk space and bandwidth.
    return image.setFormat("JPEG");
 
  }).then(function(image) {
    // Get the image data in a Buffer.
    return image.data();
 
  }).then(function(buffer) {
    // Save the image into a new file.
    var base64 = buffer.toString("base64");
    var cropped = new Parse.File("thumbnail200.jpg", { base64: base64 });
    return cropped.save();
 
  }).then(function(cropped) {
    // Attach the image file to the original object.
    photo.set("thumbnail200", cropped);
  });
};

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

    return resizePhotoAsync(photo);

  }).then(function() {
    response.success();
  }, function(error) {
    response.error(error);
  });
});

