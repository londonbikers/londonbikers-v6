function PrivateMessageHeaderUserModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = data.Id;
        self.User = new window.UserLightModel(data.User);
        self.Added = data.Added;
    } else {
        self.Id = null;
        self.User = null;
        self.Added = new Date();
    }

    self.IsValid = ko.pureComputed(function () {
        if (self.User == null || self.User == undefined) {
            return false;
        }
        if (self.Added == null) {
            return false;
        }
        // all seems good!
        return true;
    }, self);
}