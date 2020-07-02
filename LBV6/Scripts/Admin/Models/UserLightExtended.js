function UserLightExtendedModel(data) {
    var self = this;
    self.Id = data.Id;
    self.UserName = data.UserName;
    self.Joined = data.Joined;
    self.Status = data.Status;
    self.Tagline = data.Tagline;
    self.ProfileFileStoreId = data.ProfileFileStoreId;
    self.Verified = data.Verified;

    // privileged attributes
    self.Email = data.Email;
    self.Created = new Date(data.Created);
    self.FirstName = data.FirstName;
    self.LastName = data.LastName;
    self.Logins = data.Logins;
    self.TopicsCount = parseInt(data.TopicsCount);
    self.RepliesCount = parseInt(data.RepliesCount);
    self.EmailConfirmed = data.EmailConfirmed;
}