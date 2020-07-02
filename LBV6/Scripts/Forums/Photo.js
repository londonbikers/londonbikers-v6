var PhotoViewModel = function (photo, topic, reply, grid) {

    var self = this;
    self.Photo = photo;
    self.Topic = topic;
    self.Reply = reply;
    self.Post = reply ? reply : topic;
    self.Grid = grid;
    self.NewComment = ko.observable();
    self.IsPostingNewComment = ko.observable(false);
    self.IsShowingEditCaptionForm = ko.observable(false);
    self.IsSavingCaption = ko.observable(false);
    self.IsUserPhotoOwner = ko.observable(false);
    self.IsEditingCaption = ko.observable(false);
    self.CommentIsBeingDeleted = ko.observable(false);
    self.CommentBeingDeleted = ko.observable();
    self.CommentIsBeingEdited = ko.observable(false);
    self.CommentBeingEdited = ko.observable();
    self.ShowDeletePhotoConfirmationBtn = ko.observable(false);
    self.IsDeletingPhoto = ko.observable(false);

    initialiseNewComment();

    // --[ VIEW FUNCTIONS ]-----------------------------------------------

    self.IsNewCommentValid = ko.pureComputed(function () {
        if (!window.IsUserLoggedIn()) {
            return false;
        }

        if (!IsNullOrEmpty(self.NewComment().Text()) && !self.IsPostingNewComment()) {
            return true;
        }

        return false;
    });

    self.PostNewComment = function () {
        Log("PostNewComment()");
        self.IsPostingNewComment(true);
        var newCommentJson = ko.toJSON(self.NewComment());

        $.ajax({
            url: "/api/photos/UpdatePhotoComment?topicId=" + self.Topic.Id(),
            type: "POST",
            data: newCommentJson,
            contentType: "application/json;charset=utf-8",
            success: function (id) {

                self.NewComment().Id = id;

                // clone the comment so we can insert it into the list
                var comment = new PhotoCommentModel();
                comment.Id = self.NewComment().Id;
                comment.Created = self.NewComment().Created;
                comment.UserId = self.NewComment().UserId;
                comment.UserName = self.NewComment().UserName;
                comment.ProfileFileStoreId = self.NewComment().ProfileFileStoreId;
                comment.PhotoId = self.NewComment().PhotoId;
                comment.Text(self.NewComment().Text());

                // add it to the comments list
                self.Photo.Comments.push(comment);

                setTimeout(function() {
                    // wait a little bit until the comment has been added to the list then scroll into view
                    $(".lg-item.lg-current .lg-comments-container-inner-list").scrollTop($(".lg-item.lg-current .lg-comments-container-inner-list")[0].scrollHeight);
                }, 10);

                window.ShowEventMessage("success", "<b>Got it!</b> - Check out your comment below...");

                // reset the new comment object so the user can write another new one if they want
                initialiseNewComment();
                self.IsPostingNewComment(false);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.IsPostingNewComment(false);
            }
        });
    };

    self.ShowAddCaptionForm = function() {
        self.IsShowingEditCaptionForm(true);
    };

    self.SaveCaption = function() {
        Log("SaveCaption()");
        self.IsSavingCaption(true);
        var photoJson = ko.toJSON(self.Photo);

        var url = "/api/photos/UpdatePhoto?topicId=" + self.Topic.Id() + "&replyId=";
        url += self.Reply != null ? self.Reply.Id() : "0";
    
        $.ajax({
            url: url,
            type: "POST",
            data: photoJson,
            contentType: "application/json;charset=utf-8",
            success: function () {
                self.IsSavingCaption(false);
                self.IsShowingEditCaptionForm(false);
                self.IsEditingCaption(false);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.IsSavingCaption(false);
                self.IsShowingEditCaptionForm(false);
                self.IsEditingCaption(false);
            }
        });
    };

    self.ShowEditCaptionForm = function () {
        if (!self.IsUserPhotoOwner()) {
            return;
        }

        self.IsShowingEditCaptionForm(true);
        self.IsEditingCaption(true);
    };

    self.InitiateCommentDeletion = function (comment) {
        self.CommentBeingDeleted(comment);
    };

    self.CancelCommentDeletion = function() {
        self.CommentBeingDeleted(null);
    }

    self.DeleteComment = function () {

        self.CommentIsBeingDeleted(true);

        $.ajax({
            url: "/api/photos/DeletePhotoComment?photoCommentId=" + self.CommentBeingDeleted().Id + "&topicId=" + self.Topic.Id(),
            type: "DELETE",
            contentType: "application/json;charset=utf-8",
            success: function () {
                self.Photo.Comments.remove(self.CommentBeingDeleted());
                self.CommentBeingDeleted(null);
                self.CommentIsBeingDeleted(false);
                ShowEventMessage("success", "<b>Deleted!</b> - The comment is no more.");
            },
            error: function () {
                self.CommentBeingDeleted(null);
                self.CommentIsBeingDeleted(false);
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });

    };

    self.InitiateCommentEditing = function(comment) {
        self.CommentBeingEdited(comment);
    };

    self.CancelCommentEditing = function() {
        self.CommentBeingEdited(null);
    }

    self.EditComment = function () {

        self.CommentIsBeingEdited(true);
        var commentJson = ko.toJSON(self.CommentBeingEdited());

        $.ajax({
            url: "/api/photos/UpdatePhotoComment?topicId=" + self.Topic.Id(),
            type: "POST",
            data: commentJson,
            contentType: "application/json;charset=utf-8",
            success: function () {
                ShowEventMessage("success", "<b>Got it!</b> - The comment has been updated.");
                self.CommentIsBeingEdited(false);
                self.CommentBeingEdited(null);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.CommentIsBeingEdited(false);
                self.CommentBeingEdited(null);
            }
        });

    };

    /**
     * Allows a user to report a photo as in breach of the site guidelines.
     * Causes the client to generate an email.
     * @returns {} 
     */
    self.ReportPhoto = function () {
        var subject = encodeURIComponent("Photo Report");
        var body = encodeURIComponent("I'd like to report this photo: " + window.location);
        window.open("mailto:moderators@londonbikers.com?subject=" + subject + "&body=" + body);
    };

    /**
     * Causes the photo deletion confirmation buttons to show.
     * @returns {} 
     */
    self.ShowDeletePhotoConfirmation = function() {
        self.ShowDeletePhotoConfirmationBtn(true);
    };

    /**
     * Actually deletes the photo once the user has confirmed the action.
     * @returns {} 
     */
    self.DeletePhoto = function() {
        self.IsDeletingPhoto(true);
        setTimeout(function() {
            self.IsDeletingPhoto(false);
            self.ShowDeletePhotoConfirmationBtn(false);
        }, 2000);
    };

    /**
     * Hides the photo deletion buttons.
     * @returns {} 
     */
    self.CancelDeletePhoto = function() {
        self.IsDeletingPhoto(false);
        self.ShowDeletePhotoConfirmationBtn(false);
    };

    // --[ EVENTS ]-------------------------------------------------------------
    
    $(".lg-item.lg-current .photo-caption-input, .lg-item.lg-current .photo-comment-input").focusin(function() {
        self.Grid.data("lightGallery").s.keyPress = false;
    });

    $(".lg-item.lg-current .photo-caption-input, .lg-item.lg-current .photo-comment-input").focusout(function () {
        self.Grid.data("lightGallery").s.keyPress = true;
    });

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function initialiseNewComment() {
        if (!window.IsUserLoggedIn()) {
            return;
        }

        self.NewComment(new PhotoCommentModel());
        self.NewComment().Created = new Date();
        self.NewComment().UserId = window._user.Id;
        self.NewComment().UserName = window._user.UserName;
        self.NewComment().ProfileFileStoreId = window._user.ProfileFileStoreId;
        self.NewComment().PhotoId = self.Photo.Id;
    }

    function init() {

        if (self.Reply != null && window.IsUserLoggedIn() && self.Reply.UserId() === window._user.Id) {
            self.IsUserPhotoOwner(true);
        }

        if (self.Reply == null && window.IsUserLoggedIn() && self.Topic.UserId() === window._user.Id) {
            self.IsUserPhotoOwner(true);
        }
    }

    init();

};