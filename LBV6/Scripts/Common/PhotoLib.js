var _photoListingMaxWidth = 800;
var _photoListingMaxHeight = 800;

/* --[ GLOBAL FUNCTIONS ]---------------------------------- */

function PhotoResizeToBounds(photo, bounds) {

    var result = [];

    // don't enlarge
    if (photo.Width <= bounds.Width && photo.Height <= bounds.Height) {
        result.Width = photo.Width;
        result.Height = photo.Height;
        return result;
    }

    var widthScale = 0;
    var heightScale = 0;

    if (photo.Width !== 0) {
        widthScale = bounds.Width / photo.Width;
    }

    if (photo.Height !== 0) {
        heightScale = bounds.Height / photo.Height;
    }

    var scale = Math.min(widthScale, heightScale);

    result.Width = Math.round(photo.Width * scale);
    result.Height = Math.round(photo.Height * scale);

    return result;
}

function PhotoOverlayDeletePhoto(post, photo, callback) {

    if (post.Photos().length === 1 && IsNullOrEmpty(post.Content())) {
        window.ShowEventMessage("info", "Can't remove last photo when there's no post content!");
        callback(false);
        return;
    }

    var url = post.IsTopic ?
        "/api/photos/DeletePhoto?photoId=" + photo.Id + "&topicId=" + post.Id() + "&replyId=0" :
        "/api/photos/DeletePhoto?photoId=" + photo.Id + "&topicId=" + post.ParentPostId() + "&replyId=" + post.Id();

    $.ajax({
        url: url,
        type: "DELETE",
        success: function () {

            // remove photo from post
            post.Photos.remove(photo);

            // set alert message
            window.ShowEventMessage("success", "Deleted Photo!");

            callback(true);

        },
        error: function () {
            ShowEventMessage("error", window._notificationGeneralErrorText);
            callback(false);
        }
    });
}

function PhotoOverlaySaveChangesToCaption(post, photo, callback) {

    var url = post.IsTopic ?
        "/api/photos/UpdatePhoto?topicId=" + post.Id() + "&replyId=0" :
        "/api/photos/UpdatePhoto?topicId=" + post.ParentPostId() + "&replyId=" + post.Id();

    $.ajax({
        url: url,
        data: ko.toJSON(photo),
        type: "POST",
        contentType: "application/json;charset=utf-8",
        success: function () {

            Log("PhotoOverlaySaveChangesToCaption(): Success");
            window.ShowEventMessage("success", "Caption updated!");
            callback();
        },
        error: function () {

            Log("PhotoOverlaySaveChangesToCaption(): Error");
            ShowEventMessage("error", window._notificationGeneralErrorText);
            callback();
        }
    });

}

/* --[ LISTINGS ]------------------------------------------ */

function GetPhotoListingStyleWidth(photo) {
    return GetPhotoListingSize(photo).Width + "px";
}

function GetPhotoListingStyleHeight(photo) {
    return GetPhotoListingSize(photo).Height + "px";
}

function GetPhotoListingSize(photo) {
    var bounds = [];
    bounds.Width = _photoListingMaxWidth;
    bounds.Height = _photoListingMaxHeight;
    return PhotoResizeToBounds(photo, bounds);
}