var PostControlViewModel = function () {
    var self = this;

    if (window.IsUserLoggedIn()) {
        self.ShowingTopicUploader = ko.observable(false);
        self.Posting = ko.observable(false);
        self.Uploading = ko.observable(false);
        self.NewTopic = ko.observable(new window.TopicModel());
        self.NewTopic().UserId(window._user.Id);
    }

    // --[ POST FUNCTIONS ]----------------------------------------------

    self.ReadyToPostTopic = ko.pureComputed(function () {
        if (self.NewTopic == undefined) {
            return false;
        }

        var isValid = self.NewTopic().IsValid();
        var isUploading = self.Uploading();
        return isValid && !isUploading;
    }, self);

    self.SelectedForumIsGallery = ko.pureComputed(function () {
        if (self.NewTopic == undefined) {
            return false;
        }

        if (self.NewTopic().ForumId() == undefined) {
            return false;
        }

        return window.IsForumAGallery(self.NewTopic().ForumId());
    }, self);

    self.ShowTopicUploader = function () {
        self.ShowingTopicUploader(true);
        if (window.IsMobile()) {
            $(".dz-clickable").click();
        }
    };

    self.HideTopicUploader = function () {
        self.ShowingTopicUploader(false);
    };

    self.SubmitNewTopic = function () {
        self.Posting(true);
        var jsonNewTopic = ko.toJSON(self.NewTopic());

        $.ajax({
            url: "/api/topics",
            type: "POST",
            data: jsonNewTopic,
            contentType: "application/json;charset=utf-8",
            success: function (postId) {
                window.SetEventMessage("success", "<b>Got it!</b> - Check it out below...");
                self.NewTopic().Id(postId);
                window.location = self.NewTopic().GetUrl();
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.Posting(false);
            }
        });
    };

    // --[ PHOTO UPLOADS ]-----------------------------------------------

    function onUploadingFile() {
        if (self.Uploading() === false) {
            self.Uploading(true);
        }
    }

    function onUploadingFileComplete(args) {
        if (!args.accepted)
            return;

        // the response comes back in quotes
        var photoId = args.xhr.response.replace(/"/g, "");

        // we can shortcut the addition of photos to the post and reduce the request count by including them
        // in the post create request.
        // note: this will need to change if we introduce edit-in-place, which seems like the best solution as
        // posting gets more complex
        self.NewTopic().PhotoIdsToIncludeOnCreation.push(photoId);
    }

    function onUploadQueueComplete() {
        self.Uploading(false);
    }

    function initPhotoUploader() {
        // set-up the dropzone upload control - this needs doing in a programmatic fashion so we can provide a callback function
        // to allow us to capture the file ids as they're created and use those in our post.
        $("#photo-uploader-control").addClass("dropzone").dropzone({
            url: "/api/photos/UploadPhoto",
            dictDefaultMessage: window.IsMobile() ? "Click to add photos" : "click or drag photos here to upload",
            maxFilesize: 10,
            acceptedFiles: "image/*",
            processing: onUploadingFile,
            complete: onUploadingFileComplete,
            queuecomplete: onUploadQueueComplete
        });
    }

    // --[ PRIVATE FUNCTIONS ]-------------------------------------------

    function init() {

        if (!IsUserLoggedIn()) {
            return;
        }

        //Log("PostControlViewModel.init()");
        $("#post-control-modal").on("shown.bs.modal", function () { $("#new-post-subject").focus(); });
        initPhotoUploader();

        // get a list of categories the user is authorised to post in

    }

    // --[ INIT ]--------------------------------------------------------

    init();

};