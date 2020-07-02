function ShowPhotoSwipe(post, photo) {

    var items = [];
    var initialPhotoPosition = null;
    var initialBeforeChangeOccured = false;

    for (var i = 0; i < post.Photos().length; i++) {

        // we need the index of the initiating photo so it can be used as the start photo
        if (photo.Id === post.Photos()[i].Id) {
            initialPhotoPosition = i;
        }

        // the bounds need to be orientated to match the photo to ensure that the photo fits 1:1 when both orientations are the same
        // this means for the smaller orientation the image will be oversized, which is better than a small image being expanded.
        // the other consideration here is mobile devices where orientation changes. You typically start out in portrait (smaller dimensions for the average photo and move to 

        var bounds = [];
        var screenHeight = screen.height;

        if (!window.IsMobile()) {

            // just fit to bounds but reduce height to accommodate photo-swipe chrome
            screenHeight = screenHeight - 88;
            bounds.Width = screen.width;
            bounds.Height = screenHeight;

        } else {

            // for mobile work out how to display the image 1:1 along the longest dimension, i.e. portrait to portrait or landscape to landscape
            if (screen.width >= screenHeight) {

                // device is landscape or square
                if (post.Photos()[i].Width >= post.Photos()[i].Height) {

                    // device is landscape, so is photo, so return landscape bounds
                    bounds.Width = screen.width;
                    bounds.Height = screenHeight;

                } else {

                    // device is landscape, photo is portrait, so return portrait bounds
                    bounds.Width = screenHeight;
                    bounds.Height = screen.width;

                }

            } else {

                // device is portrait
                if (post.Photos()[i].Height >= post.Photos()[i].Width) {

                    // device is portrait, so is photo, so return portrait bounds
                    bounds.Height = screenHeight;
                    bounds.Width = screen.width;

                } else {

                    // device is portrait, photo is landscape, so return landscape bounds
                    bounds.Width = screenHeight;
                    bounds.Height = screen.width;

                }
            }
        }

        var imageDimensionsWithinBounds = PhotoResizeToBounds(post.Photos()[i], bounds);

        items.push({
            src: "/os/" + post.Photos()[i].FilestoreId + "?maxwidth=" + imageDimensionsWithinBounds.Width + "&maxheight=" + imageDimensionsWithinBounds.Height + "&zoom=" + window.GetPixelRatio(),
            w: imageDimensionsWithinBounds.Width,
            h: imageDimensionsWithinBounds.Height,
            title: post.Photos()[i].Caption(),
            pid: post.Photos()[i].Id
        });

    }

    var options = {
        index: initialPhotoPosition,
        shareEl: false,
        fullscreenEl: false,
        history: true,
        galleryPIDs: true,
        galleryUID: post.Id()
    };

    var photoSwipeElement = $("#photo-swipe")[0];
    var gallery = new PhotoSwipe(photoSwipeElement, PhotoSwipeUI_Default, items, options);
    var photoSwipeViewModel = new window.PhotoSwipeViewModel(post, photo, gallery);

    gallery.listen("close", function () {
        OnClosePhotoSwipe();
        $("body").removeClass("noscroll");
        photoSwipeViewModel.Close();
    });

    gallery.listen("beforeChange", function () {

        var index = gallery.getCurrentIndex();
        var photo = post.Photos()[index];
        MarkPhotoViewed(photo.Id);

        // skip the first event occurrence as this happens on init
        // and we don't want to do anything then.
        if (!initialBeforeChangeOccured) {
            Log("beforeChange: not actioning first occurrence");
            initialBeforeChangeOccured = true;
            return;
        }

        Log("setting new photo on view-model...");
        photoSwipeViewModel.Photo(photo);

    });

    gallery.init();
    ko.applyBindings(photoSwipeViewModel, photoSwipeElement);
    $("body").addClass("noscroll");

}

function OnClosePhotoSwipe() {
    Log("OnClosePhotoSwipe()");
    ko.cleanNode($("#photo-swipe")[0]);
}

/**
 * Parse picture index and gallery index from URL (#&pid=1&gid=2)
 */
var photoswipeParseHash = function () {
    var hash = window.location.hash.substring(1),
        params = {};

    if (hash.length < 5) {
        return params;
    }

    var vars = hash.split("&");
    for (var i = 0; i < vars.length; i++) {
        if (!vars[i]) {
            continue;
        }
        var pair = vars[i].split("=");
        if (pair.length < 2) {
            continue;
        }
        params[pair[0]] = pair[1];
    }

    if (params.gid) {
        params.gid = parseInt(params.gid, 10);
    }

    return params;
};