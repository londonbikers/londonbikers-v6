function ForumExtendedModel(data) {

    var self = this;
    self.AccessRoles = ko.observableArray();
    self.PostRoles = ko.observableArray();

    if (data != undefined) {
        self.Id = ko.observable(data.Id);
        self.Created = ko.observable(data.Created);
        self.Name = ko.observable(data.Name);
        self.CategoryId = ko.observable(data.CategoryId);
        self.CategoryName = ko.observable(data.CategoryName);
        self.Order = ko.observable(data.Order);
        self.PostCount = ko.observable(data.PostCount);
        self.LastUpdated = ko.observable(data.LastUpdated);

        data.AccessRoles.forEach(function(accessRoleData) {
            self.AccessRoles.push(new ForumAccessRoleModel(accessRoleData));
        });

        data.PostRoles.forEach(function (postRoleData) {
            self.PostRoles.push(new ForumPostRoleModel(postRoleData));
        });

    } else {
        self.Id = ko.observable();
        self.Created = ko.observable();
        self.Name = ko.observable();
        self.CategoryId = ko.observable();
        self.CategoryName = ko.observable();
        self.Order = ko.observable();
        self.PostCount = ko.observable();
        self.LastUpdated = ko.observable();
    }

    self.IsValid = ko.pureComputed(function () {
        if (IsNullOrEmpty(self.Name())) {
            return false;
        }
        if (self.CategoryId() == null || self.CategoryId() < 1) {
            return false;
        }
        return true;
    }, self);

    self.CanForumBeDeleted = ko.pureComputed(function () {
        // this needs real-time updates really.
        if (self.PostCount() > 0) {
            return false;
        }
        return true;
    }, self);

    self.GetUrl = ko.pureComputed(function () {
        return "/forums/" + self.Id() + "/" + window.EncodeUrlPart(self.Name());
    }, self);

    self.GetCategoryUrl = ko.pureComputed(function () {
        return "/forums/categories/" + self.CategoryId() + "/" + window.EncodeUrlPart(self.CategoryName());
    }, self);

    self.GetFriendlyLastUpdated = ko.pureComputed(function () {
        return moment(self.LastUpdated()).fromNow();
    }, self);

    self.GetFriendlyStats = ko.pureComputed(function () {
        if (self.PostCount() > 0) {
            return "<b>" + window.NumberWithCommas(self.PostCount()) + "</b> posts";
        } else {
            return "-";
        }
    }, self);
}