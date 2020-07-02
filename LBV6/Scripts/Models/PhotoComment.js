function PhotoCommentModel(data) {
    var self = this;

    if (data != null) {
        self.Id = data.Id;
        self.PhotoId = data.PhotoId;
        self.Created = data.Created;
        self.UserId = data.UserId;
        self.UserName = data.UserName;
        self.ProfileFileStoreId = data.ProfileFileStoreId;
        self.Text = ko.observable(data.Text);
    } else {
        self.Id = null;
        self.PhotoId = null;
        self.Created = new Date();
        self.UserId = null;
        self.UserName = null;
        self.ProfileFileStoreId = null;
        self.Text = ko.observable();
    }

}