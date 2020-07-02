function ForumAccessRoleModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = ko.observable(data.Id);
        self.ForumId = ko.observable(data.ForumId);
        self.Role = ko.observable(data.Role);
    } else {
        self.Id = ko.observable();
        self.ForumId = ko.observable();
        self.Role = ko.observable();
    }

    self.IsValid = ko.pureComputed(function () {
        if (IsNullOrEmpty(self.Role())) {
            return false;
        }
        if (self.ForumId() == null || self.ForumId() < 1) {
            return false;
        }
        return true;
    }, self);

}