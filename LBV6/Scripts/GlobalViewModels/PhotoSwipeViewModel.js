var PhotoSwipeViewModel = function (post, photo, photoSwipe) {

    var self = this;

    self.Post = ko.observable(post);
    self.Photo = ko.observable(photo);
    self.PhotoSwipe = photoSwipe;
    self.ProcessingAction = ko.observable(false);
    self.DeletingPhoto = ko.observable(false);
    self.ChallengingPhotoDelete = ko.observable(false);
    self.ChallengingPhotoTimer = null;
    self.ShowingEditPhotoModal = ko.observable(false);

    self.CloseEditModal = function () {
        Log("CloseEditModal()");
        $("#pswp-edit-overlay").fadeOut(100);
        self.ShowingEditPhotoModal(false);
        self.ProcessingAction(false);
        self.DeletingPhoto(false);
        self.ChallengingPhotoDelete(false);
        self.ChallengingPhotoTimer = null;
    };

    self.UpdateCaption = function() {
        Log("update caption...");
        self.ProcessingAction(true);
        window.PhotoOverlaySaveChangesToCaption(self.Post(), self.Photo(), function () {
            self.CloseEditModal();
        });
    };

    self.DeletePhoto = function(model, button) {
        Log("delete photo...");

        if (!self.ChallengingPhotoDelete()) {
            self.ProcessingAction(true);

            // they might not be sure they want to delete the photo
            button.target.innerHTML = "<b>Are you sure?</b>";
            self.ChallengingPhotoDelete(true);
            self.ChallengingPhotoTimer = setTimeout(function () {

                button.target.innerHTML = "Delete Photo";
                self.ChallengingPhotoDelete(false);
                self.ProcessingAction(false);

            }, 4000);

        } else {

            // they really want to delete the photo
            if (!IsNull(self.ChallengingPhotoTimer)) {
                Log("clearing timer");
                clearTimeout(self.ChallengingPhotoTimer);
            };

            self.DeletingPhoto(true);
            window.PhotoOverlayDeletePhoto(self.Post(), self.Photo(), handleDeletePhotoCallback);
        }
    };

    self.ShowEditModal = function () {
        Log("ShowEditModal()");
        $("#pswp-edit-overlay").fadeIn(100);
        self.ShowingEditPhotoModal(true);
        $("#pswp-edit-caption").focus();
    };

    self.Close = function () {
        Log("Close()");
        self.CloseEditModal();
        $(document).off("keyup", handleKeyPress);
        self.PhotoSwipe.close();
    };

    self.Photo.subscribe(function(newPhoto) {
        Log("Detected a new photo being loaded");
        reset();
    });

    self.CanUserEditPhoto = ko.pureComputed(function() {
        if (window.IsUserLoggedIn() && !window.IsMobile() && self.Post().UserId() === window._user.Id) {
            return true;
        }
        return false;
    }, self);

    // --[ PRIVATE FUNCTIONS ]-------------------------------------------

    function handleDeletePhotoCallback(deleteSuccessful) {
        if (deleteSuccessful) {
            self.Close();
        } else {
            reset();
        }
    }

    function handleKeyPress(e) {
        // escape key pressed
        if (e.keyCode === 27 && self.ShowingEditPhotoModal()) {
            self.CloseEditModal();
        }

        // e key pressed and not currently editing photo details
        if (e.keyCode === 69 && !self.ShowingEditPhotoModal()) {
            self.ShowEditModal();
        }
    }

    // resets state of the view-model for events such as closure or image navigation
    function reset() {
        self.CloseEditModal();
    }

    function init() {
        Log("init()");
        $(document).on("keyup", handleKeyPress);
    }

    // --[ INIT ]--------------------------------------------------------

    init();

};