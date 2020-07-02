function PrivateMessageModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = data.Id;
        self.Created = data.Created;
        self.PrivateMessageHeaderId = data.PrivateMessageHeaderId;
        self.UserId = ko.observable(data.UserId);
        self.UserName = data.UserName;
        self.ProfileFileStoreId = ko.observable(data.ProfileFileStoreId);
        self.Content = ko.observable(data.Content);

        self.Photos = ko.observableArray(data.Photos);
        data.Photos.forEach(function (postAttachment) {
            self.Photos.push(new window.PostAttachmentModel(postAttachment));
        });

        self.Type = data.Type;
        self.ReadBy = ko.observableArray();
        data.ReadBy.forEach(function (readBy) {
            self.ReadBy.push(new ReadByModel(readBy));
        });

    } else {
        self.Id = null;
        self.Created = new Date();
        self.PrivateMessageHeaderId = null;
        self.UserId = ko.observable();
        self.UserName = null;
        self.ProfileFileStoreId = ko.observable();
        self.Content = ko.observable();
        self.Photos = ko.observableArray();
        self.ReadBy = ko.observableArray();
        self.Type = "Message";
    }

    self.IsValid = ko.pureComputed(function () {
        if (IsNull(self.UserId())) {
            return false;
        }
        if (IsNullOrEmpty(self.Content())) {
            return false;
        }
        return true;
    }, self);

    self.GetFormattedContent = ko.pureComputed(function () {
        return window.FormatPost(self.Content());
    }, self);

    self.GetRelativeCreated = ko.pureComputed(function () {
        return moment(self.Created).fromNow();
    }, self);

    self.GetFriendlyCreated = ko.pureComputed(function () {
        return moment(self.Created).format("MMMM Do YYYY, h:mm:ss a");
    }, self);

    self.IsRead = ko.pureComputed(function () {

        // read-bys aren't needed for messages users themselves create
        if (self.UserId() === window._user.Id) {
            return true;
        }

        var readBy = _.find(self.ReadBy(), function (rb) { return rb.UserId === window._user.Id });
        return !IsNull(readBy);

    }, self);
}