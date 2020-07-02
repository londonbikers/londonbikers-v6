function UserModel(data) {
    var self = this;
    self.Id = data.Id;
    self.UserName = ko.observable(data.UserName);
    self.Joined = data.Joined;
    self.Tagline = ko.observable(data.Tagline);
    self.Biography = ko.observable(data.Biography);
    self.TopicsCount = ko.observable(data.TopicsCount);
    self.RepliesCount = ko.observable(data.RepliesCount);
    self.PhotosCount = ko.observable(data.PhotosCount);
    self.ModerationsCount = ko.observable(data.ModerationsCount);
    self.ProfileFileStoreId = ko.observable(data.ProfileFileStoreId);
    self.CoverFileStoreId = ko.observable(data.CoverFileStoreId);
    self.Verified = data.Verified;

    self.GetCleanBiography = ko.pureComputed(function () {

        if (window.IsNullOrEmpty(self.Biography())) {
            return null;
        }

        // remove redundant apostrophes. some weird legacy forum issue.
        var bio = self.Biography().replace(/'{2,}/gm, "'");
        bio = window.SafeFormatText(bio);
        return bio;

    }, self);
}