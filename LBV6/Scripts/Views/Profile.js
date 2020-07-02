// ---[ VARS ]------------------------------------------------------------------------------

var _popup;
var _needToShowUploadProgressPopup = false;

// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(window._payload), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (profile) {

    var self = this;
    self.Profile = ko.observable(new window.UserModel(profile));
    self.ShowingCoverPhotoButton = ko.observable(false);
    self.EditingBio = ko.observable(false);

    self.GetJoinedYear = ko.computed(function () {
        return new Date(self.Profile().Joined).getFullYear();
    }, self);

    self.ShowCoverPhotoButton = function() {
        self.ShowingCoverPhotoButton(true);
    };

    self.HideCoverPhotoButton = function () {
        self.ShowingCoverPhotoButton(false);
    };

    self.GetCoverUrlCss = ko.pureComputed(function () {

        if (window.IsNullOrEmpty(self.Profile().CoverFileStoreId())) {
            return null;
        }

        var coverContainerWidth = 1200;
        var maxScreenSize = Math.max(screen.width, screen.height);
        var imageWidth = Math.min(coverContainerWidth, maxScreenSize);
        
        var url = window.GetCoverPhotoUrl(self.Profile().CoverFileStoreId(), imageWidth);
        var css = "url(\"" + url + "\")";
        return css;

    }, self);

    self.HasCoverImage = ko.pureComputed(function () {
        return !window.IsNullOrEmpty(self.Profile().CoverFileStoreId());
    }, self);

    self.CanMessageUser = ko.pureComputed(function() {
        return window.IsUserLoggedIn() && self.Profile().Id !== window._user.Id;
    }, self);

    self.ShouldShowOwnerControls = ko.pureComputed(function() {
        return self.ShowingCoverPhotoButton() && window.IsUserLoggedIn() && self.Profile().Id === window._user.Id;
    }, self);

    self.RemoveCoverPhoto = function() {
        Log("RemoveCoverPhoto()");

        $.ajax({
            url: "/api/photos/DeleteCoverPhoto",
            type: "DELETE",
            success: function (response) {
                self.Profile(new window.UserModel(response));
                window.ShowEventMessage("success", "Cover removed!");
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });

    }

    self.EditBio = function () {

        // strip html from the legacy site biography values first so we're working with plain-text
        self.Profile().Biography(window.HtmlToText(self.Profile().Biography()));
        self.EditingBio(true);
        $("#profile-bio-editor").focus();

    };

    self.CancelEditBio = function () {
        Log("CancelEditBio()");
        self.EditingBio(false);
    };

    self.SaveBioChanges = function () {

        Log("SaveBioChanges()");
        var jsonModel = ko.toJSON(self.Profile());
        $.ajax({
            url: "/api/users/UpdateProfile",
            type: "POST",
            data: jsonModel,
            contentType: "application/json;charset=utf-8",
            success: function () {
                window.ShowEventMessage("success", "<b>Got it!</b> - Check it out below...");
                self.EditingBio(false);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.EditingBio(false);
            }
        });

    };

    self.OwnProfile = function() {
        return window.IsUserLoggedIn() && self.Profile().Id === window._user.Id;
    }

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function onUploading() {
        _needToShowUploadProgressPopup = true;

        // don't show the popup immediately, some pics might upload so quick it'll cause a flicker.
        setTimeout(function () {

            // if the upload completes before the timer finishes, don't show the popup
            if (!_needToShowUploadProgressPopup) {
                return;
            }

            _popup = new $.Popup({
                content: function () {
                    return String.format(window._loadingPopupTemplate, "Uploading...");
                }
            });
            _popup.open();

        }, 200);
    }

    function closeUploadingLoader() {
        _needToShowUploadProgressPopup = false;
        if (!IsNull(_popup)) {
            _popup.close();
        }
    }

    function onUploadingComplete(args) {
        // the response comes back in quotes
        var profileFileStoreId = args.xhr.response.replace(/"/g, "");
        self.Profile().ProfileFileStoreId(profileFileStoreId);
        closeUploadingLoader();
    }

    function onCoverUploadingComplete(args) {
        closeUploadingLoader();
    }

    function onCoverUploadingError(args) {
        var json = JSON.parse(args.xhr.response);
        window.ShowEventMessage("error", json.Message);
    }

    function onCoverUploadingSuccess(args) {
        self.Profile(new window.UserModel(JSON.parse(args.xhr.response)));
        window.ShowEventMessage("success", "Cover changed!");
    }

    function onKeyPress(e) {
        // escape key pressed
        if (e.keyCode === 27 && self.EditingBio()) {
            self.CancelEditBio();
        }
    }

    function init() {

        if (window.IsUserLoggedIn() && window._user.Id === self.Profile().Id) {
            
            // set-up the profile picture uploader
            $("#profile-photo")
                .addClass("dropzone")
                .addClass("profile-photo-clickable")
                .attr("data-toggle", "tooltip")
                .attr("data-placement", "bottom")
                .attr("title", "Change your profile picture")
                .tooltip();

            Dropzone.autoDiscover = false;
            $("#profile-photo").dropzone({
                url: "/api/users/ChangeProfilePhoto",
                maxFilesize: 5,
                acceptedFiles: "image/*",
                createImageThumbnails: false,
                previewTemplate: "<div style=\"display:none\"></div>",
                clickable: "#profile-photo-img",
                dictDefaultMessage: "",
                processing: onUploading,
                complete: onUploadingComplete
            });

            // setup cover photo uploader
            Dropzone.autoDiscover = false;
            $("#cover-photo-uploader").dropzone({
                url: "/api/photos/UploadProfileCover",
                maxFilesize: 10,
                acceptedFiles: "image/*",
                createImageThumbnails: false,
                previewTemplate: "<div style=\"display:none\"></div>",
                clickable: "#btn-upload-cover",
                dictDefaultMessage: "",
                processing: onUploading,
                complete: onCoverUploadingComplete,
                success: onCoverUploadingSuccess,
                error: onCoverUploadingError
            });

            $(document).on("keyup", onKeyPress);
        }

        window.StartHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();
};