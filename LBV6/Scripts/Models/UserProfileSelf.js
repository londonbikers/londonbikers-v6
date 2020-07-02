function UserProfileSelfModel(data) {
    var self = this;
    self.Id = data.Id;
    self.UserName = data.UserName;
    self.ProfileFileStoreId = data.ProfileFileStoreId;
    self.Preferences = data.Preferences;
    self.IsModerator = data.IsModerator;
    self.UnreadMessagesCount = data.UnreadMessagesCount;
}