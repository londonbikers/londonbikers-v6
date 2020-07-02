function ReplyModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = ko.observable(data.Id);
        self.Created = ko.observable(new Date(data.Created));
        self.Content = ko.observable(data.Content);
        self.ParentPostId = ko.observable(data.ParentPostId);
        self.UpVotes = ko.observable(data.UpVotes);
        self.DownVotes = ko.observable(data.DownVotes);
        self.UserId = ko.observable(data.UserId);
        self.UserName = ko.observable(data.UserName);
        self.UserMemberSince = data.UserMemberSince;
        self.ProfileFileStoreId = ko.observable(data.ProfileFileStoreId);
        self.UserTagline = ko.observable(data.UserTagline);
        self.Attachments = ko.observableArray(data.Attachments);
        self.ModerationHistoryItems = ko.observableArray(data.ModerationHistoryItems);
        self.SubscribeToTopic = ko.observable();

        self.IsGallery = ko.observable(data.IsGallery);
        self.ProtectPhotos = ko.observable(data.ProtectPhotos);
        self.EditedOn = ko.observable(!window.IsNullOrEmpty(data.EditedOn) ? new Date(data.EditedOn) : null);

        self.Photos = ko.observableArray();
        data.Photos.forEach(function(photo) {
            self.Photos.push(new PhotoModel(photo));
        });

        
    } else {
        self.Id = ko.observable();
        self.Created = ko.observable(new Date());
        self.Content = ko.observable();
        self.ParentPostId = ko.observable();
        self.UpVotes = ko.observable();
        self.DownVotes = ko.observable();
        self.UserId = ko.observable();
        self.UserName = ko.observable();
        self.ProfileFileStoreId = ko.observable();
        self.UserTagline = ko.observable();
        self.Attachments = ko.observable();
        self.ModerationHistoryItems = ko.observableArray();
        self.SubscribeToTopic = ko.observable(false);
        self.EditedOn = ko.observable();
        self.Photos = ko.observableArray();
        self.PhotoIdsToIncludeOnCreation = ko.observableArray();
        self.IsGallery = ko.observable(false);
        self.ProtectPhotos = ko.observable(false);
    }

    self.IsValid = ko.pureComputed(function () {
        // user id required
        // parent post id required
        // if new and PhotoIdsToIncludeOnCreation present: content optional
        // if existing and photos not present: content required

        if (IsNullOrEmpty(self.UserId())) {
            //Log("IsValid: Fail - UserId");
            return false;
        }
        
        if (IsNullOrEmpty(self.ParentPostId())) {
            //Log("IsValid: Fail - ParentPostId");
            return false;
        }

        // existing reply
        if (self.Id() > 0 && self.Photos().length === 0 && IsNullOrEmpty(self.Content())) {
            //Log("IsValid: Fail - Existing Reply, Content");
            return false;
        }

        // new reply
        if (self.Id() === undefined && self.PhotoIdsToIncludeOnCreation().length === 0 && IsNullOrEmpty(self.Content())) {
            //Log("IsValid: Fail - New Reply, Content");
            return false;
        }
       
        // all seems good!
        //Log("IsValid: Pass!");
        return true;
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
            return moment(self.EditedOn()).calendar();
        } else {
            return moment(self.EditedOn()).format("ll");
        }
    }, self);

    self.GetRelativeCreated = ko.pureComputed(function () {
        return moment(self.Created()).fromNow();
    }, self);

    self.GetFormattedContent = ko.pureComputed(function () {
        return window.FormatPost(self.Content());
    }, self);

    self.CanUserEditPost = ko.pureComputed(function () {
        return window.IsUserLoggedIn() && window._user.Id === self.UserId();
    }, self);

    self.IsPostBeingEdited = ko.observable(false);

    self.IsTopic = false;
}