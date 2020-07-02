function UserExtendedModel(data) {

    var self = this;
    self.Id = data.Id;
    self.Joined = new Date(data.Joined);
    self.EmailConfirmed = data.EmailConfirmed;
    self.TopicsCount = data.TopicsCount;
    self.RepliesCount = data.RepliesCount;
    self.PhotosCount = data.PhotosCount;
    self.VisitsCount = data.VisitsCount;
    self.ModerationsCount = data.ModerationsCount;
    self.Logins = data.Logins;
    self.Preferences = data.Preferences;

    // editable attributes
    self.UserName = ko.observable(data.UserName);
    self.Status = ko.observable(data.Status);
    self.Biography = ko.observable(data.Biography);
    self.Email = ko.observable(data.Email);
    self.Tagline = ko.observable(data.Tagline);
    self.FirstName = ko.observable(data.FirstName);
    self.LastName = ko.observable(data.LastName);
    self.Occupation = ko.observable(data.Occupation);
    self.ProfileFileStoreId = ko.observable(data.ProfileFileStoreId);
    self.CoverFileStoreId = ko.observable(data.CoverFileStoreId);
    self.Verified = ko.observable(data.Verified);

    self.GetFriendlyJoined = ko.pureComputed(function () {
        // this should show a time like:
        // 24 mins go
        // if date > 7 days then short date format
        var dateDiff = window.DateDiff(new Date(), self.Joined);
        if (dateDiff <= 7) {
            return window.moment(self.Joined).calendar();
        } else {
            return window.moment(self.Joined).format("MMMM Do YYYY, h:mm:ss a");
        }
    }, self);
}