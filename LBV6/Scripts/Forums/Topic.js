// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    // ReSharper disable once PossiblyUnassignedProperty
    $.getJSON("/api/topics/get/" + window._topicId, function (topic) {
        ko.applyBindings(new ViewModel(topic), $("#page-binding-scope")[0]);
    });
});

// update the following button to show that the user is following the topic.
// split out from the above code as it needs to fire when a user replies and chooses to subscribe.
function ChangeUiToFollowingTopic() {
    var btn = $("#follow-topic-btn");
    btn.text("following topic");
    btn.addClass("btn-primary");
    btn.attr("title", "Stop getting notifications");
    btn.tooltip("destroy");
    btn.tooltip();
}

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (topic) {

    var self = this;
    self.ActionedShowMessageByUri = ko.observable(false);
    self.Page = ko.observable(1);
    self.PageSize = 25;
    self.SubscribedToTopic = ko.observable();
    self.Topic = ko.observable(new window.TopicModel(topic));
    self.PostBeingEdited = ko.observable(new window.TopicModel());
    // Used to store the post content before it was edited so it can be put back on cancel
    self.PostBeingEditedInitialContent = "";
    self.PostBeingModerated = ko.observable();
    self.ModPostIsTopic = ko.observable();
    self.ModPostIsSticky = ko.observable();
    self.ModReason = ko.observable();
    self.ShowingReplyUploader = ko.observable(false);
    self.Posting = ko.observable(false);
    self.Uploading = ko.observable(false);
    self.ShowFollowControl = ko.observable(false);
    self.ScrollToReplyTimer = null;
    self.TopicsHub = null;
    // Used to help re-bind grid image SRCs when mobile devices are rotated
    self.PostContainerWidth = ko.observable();
    // Used to work out if we should load a particular mobile gallery on page load
    self.PageLoadMobileGalleryPostId = ko.observable();
    // Used to work out if we should load a particular mobile gallery and photo on page load
    self.PageLoadMobileGalleryPhotoId = ko.observable();
    self.ShowGalleryIntro = ko.observable(false);

    var windowHeight = $(window).height();

    if (window.IsUserLoggedIn()) {
        self.NewReply = ko.observable(new window.ReplyModel());
        self.NewReply().UserId(window._user.Id);
        self.NewReply().ParentPostId(self.Topic().Id());
        self.NewReply().SubscribeToTopic(window._user.Preferences.NewReplyNotifications);
        self.ShowFollowControl(true);
    }

    // --[ PHOTO UPLOADS ]------------------------------------------------------

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
        self.NewReply().PhotoIdsToIncludeOnCreation.push(photoId);
    }

    function onUploadQueueComplete() {
        self.Uploading(false);
    }

    if (window.IsUserLoggedIn()) {
        // set-up the dropzone upload control - this needs doing so we can provide a callback function
        // to allow us to capture the file ids as they're created and use those in our post.
        $("#reply-photo-uploader-control").addClass("dropzone").dropzone({
            url: "/api/photos/UploadPhoto",
            dictDefaultMessage: window.IsMobile() ? "Click to add photos" : "click or drag photos here to upload",
            maxFilesize: 10,
            acceptedFiles: "image/*",
            processing: onUploadingFile,
            complete: onUploadingFileComplete,
            queuecomplete: onUploadQueueComplete
        });
    }

    /* ********************************************************************** */

    /**
     * Acts as the data-source for the ko.datasource pager extension on the replies array
     * @returns {} 
     */
    self.GetReplies = function () {

        var params = {
            limit: this.pager.limit(),
            startIndex: this.pager.limit() * (this.pager.page() - 1)
        };
        $.ajax({
            type: "GET",
            url: "/api/replies/GetRepliesForTopic?topicId=" + window._topicId + "&limit=" + params.limit + "&startIndex=" + params.startIndex,
            context: this,
            dataType: "json",
            success: function (data) {

                // convert the json entities to models
                var replyModels = [];
                for (var i = 0; i < data.Posts.length; i++) {

                    // is this reply one we're instructed to show a gallery photo for?
                    var reply = new window.ReplyModel(data.Posts[i]);
                    if (reply.Id() === self.PageLoadMobileGalleryPostId()) {
                        loadMobileGalleryFromUrl(reply);
                    }

                    replyModels.push(reply);
                }

                // what is this voodoo? i think this adds the reply models to the page-able observable array
                this(replyModels);

                this.pager.totalCount(data.TotalPosts);

                // do we have a highlight-reply instruction?
                var hid = parseInt(window.QueryString.hid);
                if (hid > 0) {
                    $("body").waitForImages(function () {
                        Log("moving to reply");
                        $("#r-" + hid).goTo(replyHighlightCallback, true);
                    });
                    RemoveParameterFromUrl("hid");
                }

                checkforOtherDeviceTypeGalleryInstruction();
            }
        });
    };

    /**
     * Gives a reply a visual highlight for a short time, i.e. to highlight an unread reply
     * @returns {} 
     */
    self.SubmitNewReply = function () {

        self.Posting(true);
        var jsonNewReply = ko.toJSON(self.NewReply());

        $.ajax({
            url: "/api/replies",
            type: "POST",
            data: jsonNewReply,
            contentType: "application/json;charset=utf-8",
            success: function (response) {

                // navigate to the last page to show the new post or 
                // re-initialise the replies if already on that page.
                var replyOnPage = Math.ceil(response.TopicReplyCount / self.PageSize);
                if (replyOnPage === self.Replies.pager.page()) {
                    self.Replies.refresh();
                } else {
                    self.Replies.pager.specific(replyOnPage);
                }

                // scroll to the bottom to show the reply
                // hahahahahaha yes, I know, setTimeout, but I don't know when the element is going to exist. I tried.
                self.ScrollToReplyTimer = setTimeout(function() {
                    window.scrollTo(0, document.body.scrollHeight);
                }, 10);
                window.ShowEventMessage("success", "<b>Got it!</b> - Check out your reply below...");

                // update the "following" state if necessary
                if (self.NewReply().SubscribeToTopic() && $("#follow-topic-btn").text() === "follow topic") {
                    ChangeUiToFollowingTopic();
                }

                // reset reply form
                self.NewReply(new window.ReplyModel());
                self.NewReply().UserId(window._user.Id);
                self.NewReply().ParentPostId(self.Topic().Id());
                self.NewReply().SubscribeToTopic(window._user.Preferences.NewReplyNotifications);

                // reset file up-loader
                Dropzone.forElement("#reply-photo-uploader-control").removeAllFiles();
                self.ShowingReplyUploader(false);
                self.Posting(false);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.Posting(false);
            }
        });
    };

    self.OnPageChange = function () {
        self.UpdateAddress();
        $(window).scrollTop(0);
    };

    /**
     * Updates the URL in the browser's address bar upon Ajax navigation so that bookmarks and link shares work
     * @returns {} 
     */
    self.UpdateAddress = function () {
        self.Page(self.Replies.pager.page());
        var url = self.Topic().GetUrl();
        if (self.Page() > 1) {
            url += "?p=" + self.Page();
        }
        window.ChangeUrl(url);
    };

    self.ShowModerationModal = function (post) {
        self.PostBeingModerated(post);
        if (self.PostBeingModerated().ParentPostId == undefined) {
            self.ModPostIsTopic(true);
            self.ModPostIsSticky(post.IsSticky());
        } else {
            self.ModPostIsTopic(false);
        }
        
        // reset form
        $("input[name=modType]").removeAttr("checked");
        self.ModReason(null);
        $("#mod-modal").modal();
    };

    self.IsModerationFormValid = ko.pureComputed(function () {
        if (!window.IsNullOrEmpty(self.ModReason())) {
            return true;
        } else {
            return false;
        }
    }, self);
    
    /**
     * Sticks or unsticks a topic from the top of the index in the forum
     * @returns {} 
     */
    self.ToggleIsStickyModeration = function () {
        var isSticky = $("#modIsSticky:checked").length > 0;
        self.PostBeingModerated().IsSticky(isSticky);
        self.UpdatePost(self.PostBeingModerated);
        $("#mod-modal").modal("hide");
    };
    
    /**
     * Persists any changes to a post to the server
     * @param {} post 
     * @returns {} 
     */
    self.UpdatePost = function (post) {

        var url = post().IsTopic ? "/api/topics" : "/api/replies";
        var jsonPost = ko.toJSON(post());

        // to reduce the chance of duplicate saves, disable the submit button until the request is complete.
        $("#btn-save-changes").attr("disabled", "disabled");
        
        $.ajax({
            url: url,
            type: "POST",
            data: jsonPost,
            contentType: "application/json;charset=utf-8",
            success: function () {
                ShowEventMessage("success", "Post updated!");
                $("#btn-save-changes").removeAttr("disabled");
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                $("#btn-save-changes").removeAttr("disabled");
            }
        });
    };

    /**
     * Subscribes or unsubscribes a user from topic reply notifications
     * @returns {} 
     */
    self.ToggleSubscribeToTopic = function () {

        var btn = $("#follow-topic-btn");
        btn.attr("disabled", "disabled");
        if (btn.text() === "follow topic") {
            $.post("/api/topics/subscribe?id=" + self.Topic().Id(), function() {
                // success
                ChangeUiToFollowingTopic();
                btn.removeAttr("disabled");
                ShowEventMessage("success", "You're now subscribed!");
            });
        } else {
            $.post("/api/topics/unsubscribe?id=" + self.Topic().Id(), function () {
                // success
                btn.removeAttr("disabled");
                btn.text("follow topic").removeClass("btn-primary").attr("title", "Get reply notifications");
                btn.tooltip("destroy");
                btn.tooltip();
                ShowEventMessage("success", "You're now un-subscribed!");
            });
        }
    };

    self.SubmitPostModeration = function () {

        // this validation should be done IsModerationFormValid() but I couldn"t get it working 
        if ($("input[name=modType]:checked").length === 0) {
            alert("Choose a moderation action first.");
            return;
        }

        var modType = $("input[name=modType]:checked").val();
        var controller = self.ModPostIsTopic() ? "topics" : "replies";
        if (modType === "Remove") {

            $.post("/api/" + controller + "/moderator_remove/", { PostId: self.PostBeingModerated().Id(), Reason: self.ModReason() }, function () {
                if (self.ModPostIsTopic()) {
                    // we should go back to the forum the post was in when removing a topic
                    window.location.href = self.PostBeingModerated().GetForumUrl();
                } else {
                    // we can just reload the page when removing a reply
                    location.reload();
                }
            }).fail(function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                $("#mod-modal").modal("hide");
            });

        } else if (modType === "Close") {

            $.post("/api/topics/moderator_close", { PostId: self.PostBeingModerated().Id(), Reason: self.ModReason() }, function () {
                location.reload();
            }).fail(function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                $("#mod-modal").modal("hide");
            });

        } else if (modType === "Move") {

            var destinationForumId = $("#modDestinationForumId").val();
            $.post("/api/topics/moderator_move", { PostId: self.PostBeingModerated().Id(), Reason: self.ModReason(), DestinationForumId: destinationForumId }, function () {
                location.reload();
            }).fail(function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                $("#mod-modal").modal("hide");
            });

        }
    };

    self.ShowEditPostModal = function (post) {
        post.IsPostBeingEdited(true);
        self.PostBeingEdited(post);
        self.PostBeingEditedInitialContent = post.Content();
        $("#edit-modal").modal();
    };

    self.SubmitPostEdit = function () {
        self.PostBeingEdited().EditedOn(new Date());
        self.UpdatePost(self.PostBeingEdited);
        $("#edit-modal").modal("hide");
        self.PostBeingEditedInitialContent = null;
    };

    self.CancelPostEdit = function() {
        if (self.PostBeingEdited().Content() !== self.PostBeingEditedInitialContent) {
            self.PostBeingEdited().Content(self.PostBeingEditedInitialContent);
        }
    };

    self.QuotePost = function (post) {

        // build the quote...
        // for plain-text users we need to build BB code and insert it in the reply text area
        // for rich-text users we need to build the html and insert it in the redactor control

        if (window.IsMobile()) {

            var text = window.IsContentHtml(post.Content()) ? window.HtmlToText(post.Content()) : post.Content();
            var plainTextQuote = "[quote]" + text + "[hr]" + post.UserName() + "[/quote]\n";
            $("#post-ctrl-div textarea").focus().val(plainTextQuote);

        } else {

            var richTextQuote = "<blockquote><p>" + post.Content() + "</p><footer>" + post.UserName() + "</footer></blockquote>";
            var name = $("#post-ctrl-div textarea")[0].name;
            var textarea = $("#post-ctrl-div textarea[name=\"" + name + "\"]");
            textarea.redactor("insert.html", richTextQuote);
            textarea.redactor("focus.end");
        }

        // scroll to the reply editor
        $("#post-ctrl-div").goTo(null, true);
    };

    self.ShowReplyUploader = function () {

        self.ShowingReplyUploader(true);
        $("#reply-photo-uploader").goTo(null, true);

        if (window.IsMobile()) {
            $(".dz-clickable").click();
        }

    };

    self.HideReplyUploader = function () {
        self.ShowingReplyUploader(false);
    };

    self.ReadyToPostReply = ko.pureComputed(function () {

        if (IsNull(self.NewReply)) {
            return false;
        }

        var isValid = self.NewReply().IsValid();
        var isUploading = self.Uploading();
        return isValid && !isUploading;

    }, self);

    self.GetMobileGridItemPhotoSrc = function (fileStoreId) {
        var photoWidth = Math.ceil(self.PostContainerWidth() / 3);
        return window.GetPhotoUrl(fileStoreId, photoWidth);        
    };

    // --[ CUSTOM BINDINGS ]----------------------------------------------------

    /**
     * Handles post-processing of small sequence of photos, ie.. wiring up the LightGallery.
     */
    ko.bindingHandlers.bindSmallPhotoSequence = {
        update: function (element, valueAccessor) {
            var photos = ko.utils.unwrapObservable(valueAccessor()); //grab a dependency to the obs array

            if (photos === undefined || photos === null || photos.length === 0) {
                Log("initLightGallery() - no photos, not running.");
                return;
            }

            if (photos.length > 3) {
                Log("initLightGallery() - too many photos, not running.");
                return;
            }

            // make sure KO has finished rendering all the items
            var anchor = $(element);
            if (anchor.children().length !== photos.length) {
                return;
            }

            // if we're mobile, we don't want to be using light-gallery so duck out here
            if (window.IsMobile()) {
                return;
            }

            Log("initLightGallery()");
            initLightGallery(photos, anchor, ".small-sequence-item-link");
        }
    }

    ko.bindingHandlers.bindMasonry = {
        update: function (element, valueAccessor) {

            if (self.PostContainerWidth() == null) {
                self.PostContainerWidth($(".post-container").innerWidth());
            }

            // get the topic or reply photos this grid is binding against
            var photos = ko.unwrap(valueAccessor());
            if (photos === undefined || photos === null || photos.length === 0) {
                return;
            }

            var grid = $(element);

            // ReSharper disable once PossiblyUnassignedProperty
            grid.masonry({
                itemSelector: ".grid-item",
                columnWidth: 400
            });

            // layout Masonry after each image loads
            // ReSharper disable once PossiblyUnassignedProperty
            grid.imagesLoaded().progress(function () {
                // ReSharper disable once PossiblyUnassignedProperty
                grid.masonry("layout");
            });

            // if we're mobile, we don't want to be using light-gallery so duck out here
            if (window.IsMobile()) {
                return;
            }

            initLightGallery(photos, grid);
        }
    };

    // --[ EVENT HANDLERS ]-----------------------------------------------------
    
    /**
     * Event handles ensuring any photo grids are reloaded so the images display
     * correctly, both in terms of layout and pixel density.
     */
    $(window).on("orientationchange", function () {

        if (!window.IsMobile()) {
            return;
        }

        self.PostContainerWidth($(".post-container").innerWidth());
        createMobileGridItemImgClass();

        var grids = $(".grid");
        grids.each(function () {
            var grid = $(this);
            // ReSharper disable once PossiblyUnassignedProperty
            grid.masonry("reloadItems");
        });

    });

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    /**
     * Initialises a LightGallery plugin instance to a HTML element using a collection of photos from a post.
     * @param {any} photos
     * @param {any} element
     * @param {any} selectorName
     */
    function initLightGallery(photos, element, selectorName) {
        // not sure if this is the best place to do this, might
        // be able to do this up-front before binding...

        var options = {
            appendSubHtmlTo: ".lg-item",
            addClass: "fb-comments",
            mode: "lg-fade",
            download: false,
            enableDrag: false,
            enableSwipe: false,
            galleryId: photos[0].PostId
        }

        if (selectorName) {
            options.selector = selectorName;
        }

        element.lightGallery(options);

        element.on("onAfterOpen.lg", function () {
            $("body").addClass("no-scroll");
            $(window).resize(handleWindowResizeForPhoto);
            setPhotoCommentsPaneHeight();
        });

        element.on("onBeforeClose.lg", function () {
            $("body").removeClass("no-scroll");
            $(window).off("resize");
        });

        element.on("onAfterSlide.lg", function (event, prevIndex, index) {
            Log("onAfterSlide.lg...");
            var photo = photos[index];
            var reply = null;
            if (self.Topic().Id() !== photo.PostId) {
                for (var i = 0; i < self.Replies().length; i++) {
                    if (self.Replies()[i].Id() === photo.PostId) {
                        reply = self.Replies()[i];
                        break;
                    }
                }
            }

            var lgCommentsContainer = $(".lg-item.lg-current .lg-comments-container");
            if (lgCommentsContainer.length === 1) {
                if (!ko.dataFor(lgCommentsContainer[0])) {
                    bindGallery(element, photo, reply);
                }
            } else {
                // there seems to be a delay when looping, so pause for a bit
                setTimeout(function () {
                    lgCommentsContainer = $(".lg-item.lg-current .lg-comments-container");
                    if (!ko.dataFor(lgCommentsContainer[0])) {
                        bindGallery(element, photo, reply);
                    }
                }, 150);
            }

            // some forums can be set to have all topic photos protected, so let's disable the right-click menu so they can't try and save the image.
            // this obviously isn't foolproof, but it stops unskilled users from saving the image.
            if (self.Topic().ProtectPhotos()) {
                $(".lg-image").bind("contextmenu", function(e) { e.preventDefault(); });
            }

            setPhotoCommentsPaneHeight();

            MarkPhotoViewed(photo.Id);

        });
    }

    /**
     * As mobile and desktop experiences use different galleries, and those gallery plugins
     * use different hash parameter syntaxes, we need a way of converting URLs from one to the other
     * so that if someone shares a link from a desktop device to a mobile device and visa-versa, the
     * gallery continues to load as expected.
     */
    function checkforOtherDeviceTypeGalleryInstruction() {

        var baseUrl = window.location.protocol + "//" + window.location.hostname + window.location.pathname;
        if (!window.IsMobile()) {

            // desktop:
            // does the URL contain instructions to load a mobile photo gallery?
            var mobileGalleryParams = window.photoswipeParseHash();
            if (mobileGalleryParams.gid && mobileGalleryParams.pid) {
                var post = self.Topic().Id() === mobileGalleryParams.gid ? self.Topic() : getReplyFromViewModel(mobileGalleryParams.gid);
                var slide = getPhotoIndex(post, mobileGalleryParams.pid);
                var url = baseUrl + "#lg=" + mobileGalleryParams.gid + "&slide=" + slide;
                var conversionUrl = "/forums/posts-conversion?url=" + url;
                window.location = conversionUrl;
            }

        } else {

            // mobile:
            // does the URL contain instructions to load a desktop photo gallery?
            if (window.location.hash && window.location.hash.startsWith("#lg=")) {
                var galleryIdRegex = /lg=(.*?)&/gi;
                var galleryId = parseInt(galleryIdRegex.exec(window.location.hash)[1]);
                var photoIndexRegex = /slide=(.*)/gi;
                var photoIndex = parseInt(photoIndexRegex.exec(window.location.hash)[1]);
                var post2 = self.Topic().Id() === galleryId ? self.Topic() : getReplyFromViewModel(galleryId);
                var photoId = post2.Photos()[photoIndex].Id;

                var url2 = baseUrl + "#&gid=" + galleryId + "&pid=" + photoId;
                var conversionUrl2 = "/forums/posts-conversion?url=" + url2;
                window.location = conversionUrl2;
            }

        }
    }

    /**
     * Finds and returns a reply object from the view model self.Replies observable array by its id.
     * Returns null if no such reply object was found.
     * @param {integer} replyId
     */
    function getReplyFromViewModel(replyId) {
        for (var i = 0; i < self.Replies().length; i++) {
            if (self.Replies()[i].Id() === replyId) {
                return self.Replies()[i];
            }
        }
        return null;
    }

    /**
     * Finds out the index of a photo in the post Photos array.
     * Returns null if the photo cannot be found.
     * @param {} post - A Post model.
     * @param {integer} photoId - The id of the photo to find the index for.
     */
    function getPhotoIndex(post, photoId) {
        for (var i = 0; i < post.Photos().length; i++) {
            if (post.Photos()[i].Id === photoId) {
                return i;
            }
        }
        return null;
    }

    /**
     * When a mobile gallery load instruction is received from the URL and converted to params, this
     * function can be used to find the post (topic or reply) and photo, then load the PhotoSwipe gallery.
     * @param {any} post
     */
    function loadMobileGalleryFromUrl(post) {
        // the post has been handed to us, as the calling code has identified when the post has been loaded, i.e. on page load
        // if it was for the topic, or when replies were loaded. we have to find the photo now.

        // we could have this parameterless and find the post on our own, but that would require waiting until all replies are loaded
        // this way the rendering code can tell us as soon as we've loaded the post and show the gallery quicker.

        // find the photo...
        for (var i = 0; i < post.Photos().length; i++) {
            if (post.Photos()[i].Id === self.PageLoadMobileGalleryPhotoId()) {
                window.ShowPhotoSwipe(post, post.Photos()[i]);
                break;
            }
        }
    }

    /**
     * The CSS class needs creating dynamically to fit the device screen size
     * and must be done before the grids are initialised.
     * @returns {} 
     */
    function createMobileGridItemImgClass() {
        Log("createMobileGridItemImgClass()");

        // determine the new grid item size
        var postContainerWidth = screen.width - 40;
        var gridItemWidth = Math.ceil(postContainerWidth / 3);

        // remove any existing rule
        $("#grid-item-img-dyn-style").remove();

        // create the style rule
        var css = "<style type=\"text/css\" id=\"grid-item-img-dyn-style\">.grid-item-img-dyn { width:" + gridItemWidth + "px; }</style>";
        $(css).appendTo("head");
    }

    function bindGallery(grid, photo, reply) {
        var lgCommentsContainer = $(".lg-item.lg-current .lg-comments-container");
        ko.applyBindings(new PhotoViewModel(photo, self.Topic(), reply, grid), lgCommentsContainer[0]);
    }

    /**
     * as the window resizes, this will ensure the comments pane resizes accordingly.
     * this is required to enable vertical scrolling within the comment pane.
     */
    function handleWindowResizeForPhoto() {
        if ($(window).height() === windowHeight) {
            //Log("window hasn't changed height.");
            return;
        }

        windowHeight = $(window).height();
        setPhotoCommentsPaneHeight();
    }

    function setPhotoCommentsPaneHeight() {
        //Log("setPhotoCommentsPaneHeight()");
        var container = $(".lg-item.lg-current .lg-comments-container-inner");
        var header = $(".lg-item.lg-current .lg-comments-container-inner-header");
        var poster = $(".lg-item.lg-current .lg-comments-container-inner-poster");
        var list = $(".lg-item.lg-current .lg-comments-container-inner-list");

        // 30 is the top and bottom padding
        var spaceAvailable = container.innerHeight() - header.outerHeight() - poster.outerHeight() - 30; 
        list.height(spaceAvailable);
    }

    function receiveUpdatedTopic(updatedTopicDto) {
        Log("receiveUpdatedTopic()");
        self.Topic(new window.TopicModel(updatedTopicDto));
    }

    function removeTopic() {
        // redirect back to the topic forum
        SetEventMessage("info", "Sorry, that topic has been removed.");
        window.location = GetForumUrl(self.Topic().ForumId(), self.Topic().ForumName());
    }

    function receiveNewReply(newReplyDto) {
        Log("receiveNewReply()");

        // task:
        // receive new total posts count
        // receive new reply, convert to model
        // ...
        // update this.pager.totalCount(data.TotalPosts);
        // if page === total pages and item count <= page size, add item to UI and highlight
        // otherwise add reply to list so it can be navigated to at users will

        self.Replies.pager.totalCount(newReplyDto.TotalReplies);

        if (self.Replies.pager.isLastPage()) {
            Log("still on last page, so can show new reply");
            self.Replies.refresh();
        }
    }

    function receiveUpdatedReply(updatedReplyDto) {

        // do we have the reply in the view? if so, swap it out
        var currentReply = self.Replies().find(function (r) {
            return r.Id() === updatedReplyDto.Id;
        });

        if (IsNull(currentReply)) {
            Log("Updated reply not found in replies list. Stopping.");
        }

        var updatedReplyModel = new window.ReplyModel(updatedReplyDto);
        self.Replies.replace(currentReply, updatedReplyModel);
        Log("Replaced reply");

    }

    function removeReply(removedReplyId) {

        // do we have the reply in the view? if so remove it
        var currentReply = self.Replies().find(function (r) {
            return parseInt(r.Id()) === parseInt(removedReplyId);
        });

        if (IsNull(currentReply)) {
            Log("Removed reply not found in replies list. Stopping.");
        }

        self.Replies.refresh();
        Log("Removed reply");
    }

    function replyHighlightCallback(element) {
        // weirdly this can be run twice, some issue with the animate function in goTo();
        if (self.ActionedShowMessageByUri()) {
            return;
        }

        // whatever happens, we don't want to run this again
        self.ActionedShowMessageByUri(true);

        setTimeout(function () {
            element.addClass("reply-highlight");
        }, 500);
    }

    function registerWithHub() {

        self.TopicsHub = $.connection.topicsHub;
        self.TopicsHub.client.receiveUpdatedTopic = receiveUpdatedTopic;
        self.TopicsHub.client.removeTopic = removeTopic;
        self.TopicsHub.client.receiveNewReply = receiveNewReply;
        self.TopicsHub.client.receiveUpdatedReply = receiveUpdatedReply;
        self.TopicsHub.client.removeReply = removeReply;

        // this needs to run only when the hub has been started
        window._postHubStartTasks.push(function () {
            self.TopicsHub.server.viewingTopic(self.Topic().Id());
        });

        window.StartHub();
    }

    function init() {

        createMobileGridItemImgClass();

        // get subscription status
        // this could change in the future to be info included with the model once we get the performance profile where
        // it needs to be for notifications for this purpose.
        // ReSharper disable once PossiblyUnassignedProperty
        $.getJSON("/api/topics/is_subscribed?id=" + window._topicId, function (isSubscribed) {
            if (isSubscribed) {
                ChangeUiToFollowingTopic();
            }
        });

        // do we have a page instruction in the URL?
        self.Page(parseInt(window.QueryString.p) || 1);

        registerWithHub();
        
        if (window.IsMobile()) {
            // does the URL contain instructions to load a mobile photo gallery?
            var params = window.photoswipeParseHash();
            if (params.gid && params.pid) {
                self.PageLoadMobileGalleryPostId(params.gid);
                self.PageLoadMobileGalleryPhotoId(params.pid);
                if (self.Topic().Id() === self.PageLoadMobileGalleryPostId()) {
                    loadMobileGalleryFromUrl(self.Topic());
                }
            }
        }
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

    self.Replies = ko.observableArray([]).extend({
        datasource: self.GetReplies,
        pager: {
            limit: self.PageSize,
            startPage: self.Page(),
            onPageChange: self.OnPageChange
        }
    });
};