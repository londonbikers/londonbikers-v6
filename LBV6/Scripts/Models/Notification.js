function NotificationModel(data) {
    var self = this;

    self.Id = data.Id;
    self.ParentName = ko.observable(data.ParentName);
    self.ContentName = ko.observable(data.ContentName);
    self.ContentUrl = ko.observable(data.ContentUrl);
    self.ContentFilestoreId = ko.observable(data.ContentFilestoreId);
    self.ScenarioType = parseInt(data.ScenarioType);

    self.Users = ko.observableArray();
    data.Users.forEach(function (user) {
        self.Users.push(new UserLightModel(user));
    });

    self.Occurances = ko.observable(parseInt(data.Occurances));
    self.IsOwnContent = ko.observable(data.IsOwnContent);
    
    self.GetNotificationTemplateName = ko.pureComputed(function() {

        if (self.ScenarioType === 1) {
            return "notifications-forum-post";
        } else if (self.ScenarioType === 2 && self.Occurances() === 1 && self.IsOwnContent() === false) {
            return "notifications-post-reply";
        } else if (self.ScenarioType === 2 && self.Occurances() === 1 && self.IsOwnContent() === true) {
            return "notifications-replied-to-your-post";
        } else if (self.ScenarioType === 2 && self.Occurances() > 1 && self.IsOwnContent() === false) {
            return "notifications-new-post-replies";
        } else if (self.ScenarioType === 2 && self.Occurances() > 1 && self.IsOwnContent() === true) {
            return "notifications-your-post-has-replies";
        } else if (self.ScenarioType === 6 && self.Occurances() === 1) {
            return "notifications-photo-comment";
        } else if (self.ScenarioType === 6 && self.Occurances() > 1) {
            return "notifications-photo-comments";
        }

        Log("Unknown notification scenario");
        return null;

    }, self);

    self.GetProfilePictures = function () {
        if (self.Users().length === 1) {
            return window.GetUserProfileGraphic("medium", self.Users()[0].UserName, self.Users()[0].ProfileFileStoreId);
        } else {
            // scenario not yet supported
            Log("NotificationModel: Multiple users not yet supported.");
            return "";
        }
    }
}