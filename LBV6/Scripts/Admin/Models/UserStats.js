function UserStatsModel(data) {
    var self = this;

    self.TotalUsers = data.TotalUsers;
    self.EnabledUsers = data.TotalUsers;
    self.SuspendedUsers = data.SuspendedUsers;
    self.BannedUsers = data.BannedUsers;
    self.ConfirmedUsers = data.ConfirmedUsers;
    self.FacebookLogins = data.FacebookLogins;
    self.GoogleLogins = data.GoogleLogins;
}