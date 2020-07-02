function ProminentUserModel(data) {
    var self = this;

    if (data != undefined) {
        self.UserName = data.UserName;
        self.ProfileFileStoreId = data.ProfileFileStoreId;
        self.Reason = data.Reason;
    }
}