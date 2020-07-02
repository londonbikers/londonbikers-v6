function TopicModel(data) {
    var self = this;

    if (data != undefined) {

        self.Id = ko.observable(data.Id);
        self.Created = ko.observable(new Date(data.Created));
        self.Subject = ko.observable(data.Subject);
        self.Content = ko.observable(data.Content);
        self.CategoryId = ko.observable(data.CategoryId);
        self.CategoryName = ko.observable(data.CategoryName);
        self.ForumId = ko.observable(data.ForumId);
        self.ForumName = ko.observable(data.ForumName);
        self.IsSticky = ko.observable(data.IsSticky);
        self.UpVotes = ko.observable(data.UpVotes);
        self.DownVotes = ko.observable(data.DownVotes);
        self.UserId = ko.observable(data.UserId);
        self.UserName = ko.observable(data.UserName);
        self.UserMemberSince = data.UserMemberSince;
        self.ProfileFileStoreId = ko.observable(data.ProfileFileStoreId);
        self.UserTagline = ko.observable(data.UserTagline);
        self.Attachments = ko.observableArray(data.Attachments);
        self.ModerationHistoryItems = ko.observableArray(data.ModerationHistoryItems);
        self.StatusCode = ko.observable(data.StatusCode);
        self.IsGallery = ko.observable(data.IsGallery);
        self.ProtectPhotos = ko.observable(data.ProtectPhotos);
        self.EditedOn = ko.observable(!window.IsNullOrEmpty(data.EditedOn) ? new Date(data.EditedOn) : null);
        self.Photos = ko.observableArray();
        data.Photos.forEach(function (photo) {
            self.Photos.push(new PhotoModel(photo));
        });

    } else {

        self.Id = ko.observable();
        self.Created = ko.observable(new Date());
        self.Subject = ko.observable();
        self.Content = ko.observable();
        self.CategoryId = ko.observable();
        self.CategoryName = ko.observable();
        self.ForumId = ko.observable();
        self.ForumName = ko.observable();
        self.IsSticky = ko.observable();
        self.UpVotes = ko.observable();
        self.DownVotes = ko.observable();
        self.UserId = ko.observable();
        self.UserName = ko.observable();
        self.ProfileFileStoreId = ko.observable();
        self.UserTagline = ko.observable();
        self.Attachments = ko.observable();
        self.ModerationHistoryItems = ko.observableArray();
        self.StatusCode = ko.observable();
        self.EditedOn = ko.observable();
        self.Photos = ko.observableArray();
        self.PhotoIdsToIncludeOnCreation = ko.observableArray();
        self.IsGallery = ko.observable(false);
        self.ProtectPhotos = ko.observable(false);

        // used just for creates to enable all photos to be tagged with a single credit.
        self.PhotoCredits = ko.observable();
    }

    self.IsValid = ko.pureComputed(function () {

        // user id required
        // subject is required
        // forum id is required
        // if forum is a gallery: photos are required
        // if new and PhotoIdsToIncludeOnCreation present: content optional
        // if existing and photos not present: content required

        if (IsNullOrEmpty(self.ForumId())) {
            return false;
        }

        if (self.Id() === undefined && window.IsForumAGallery(self.ForumId()) && self.PhotoIdsToIncludeOnCreation().length === 0) {
            Log("Missing gallery photos for new post.");
            return false;
        }

        if (self.Id() > 0 && window.IsForumAGallery(self.ForumId()) && self.Photos().length === 0) {
            Log("Missing gallery photos for existing post.");
            return false;
        }

        if (IsNullOrEmpty(self.Subject())) {
            return false;
        }

        if (IsNullOrEmpty(self.UserId())) {
            return false;
        }

        // existing reply
        if (self.Id() > 0 && self.Photos().length === 0 && IsNullOrEmpty(self.Content())) {
            return false;
        }

        // new reply
        if (self.Id() === undefined && self.PhotoIdsToIncludeOnCreation().length === 0 && IsNullOrEmpty(self.Content())) {
            return false;
        }

        // all seems good!
        return true;
    }, self);

    self.GetUrl = ko.pureComputed(function () {
        return "/forums/posts/" + self.Id() + "/" + window.EncodeUrlPart(self.Subject());
    }, self);

    self.GetCategoryUrl = ko.pureComputed(function () {
        return "/forums/categories/" + self.CategoryId() + "/" + window.EncodeUrlPart(self.CategoryName());
    }, self);

    self.GetForumUrl = ko.pureComputed(function () {
        return "/forums/" + self.ForumId() + "/" + window.EncodeUrlPart(self.ForumName());
    }, self);

    self.GetFriendlyCreated = ko.pureComputed(function () {
        // this should show a time like:
        // 24 mins go
        // if date > 7 days then short date format
        var dateDiff = window.DateDiff(new Date(), self.Created());
        if (dateDiff <= 7) {
            return window.moment(self.Created()).calendar();
        } else {
            return window.moment(self.Created()).format("ll");
        }
    }, self);

    self.GetFriendlyEditedOn = ko.pureComputed(function () {
        // this should show a time like:
        // 24 mins go
        // if date > 7 days then short date format

        if (self.EditedOn() == null) {
            return null;
        }

        var dateDiff = window.DateDiff(new Date(), self.EditedOn());
        if (dateDiff <= 7) {
            return window.moment(self.EditedOn()).calendar();
        } else {
            return window.moment(self.EditedOn()).format("ll");
        }
    }, self);

    self.GetRelativeCreated = ko.pureComputed(function () {
        return window.moment(self.Created()).fromNow();
    }, self);

    self.GetFormattedContent = ko.pureComputed(function () {
        return window.FormatPost(self.Content());
    }, self);

    self.CanUserEditPost = ko.pureComputed(function () {
        return window.IsUserLoggedIn() && window._user.Id === self.UserId();
    }, self);

    self.IsPostBeingEdited = ko.observable(false);

    self.IsTopic = true;
}