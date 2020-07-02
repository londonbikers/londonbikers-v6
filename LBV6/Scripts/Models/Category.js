function CategoryModel(data, forumModelToUse) {

    var self = this;
    self.NewForum = ko.observable(new ForumModel());
    self.Forums = ko.observableArray();

    if (data != undefined) {
        self.Id = ko.observable(data.Id);
        self.NewForum().CategoryId(data.Id);
        self.Created = ko.observable(data.Created);
        self.Name = ko.observable(data.Name);
        self.Description = ko.observable(data.Description);
        self.Order = ko.observable(data.Order);
        self.IsGalleryCategory = ko.observable(data.IsGalleryCategory);

        if (forumModelToUse) {
            // we've been given a specific forum model to use
            // most likely it's an extended one for admin use
            data.Forums.forEach(function(forumData) {
                // ReSharper disable once InconsistentNaming - ReSharper doesn't seem to understand that we're passing dealing with a referenced object type
                self.Forums.push(new forumModelToUse(forumData));
            });
        } else {
            // we've not been given a specific forum model so just use the regular one
            data.Forums.forEach(function (forumData) {
                self.Forums.push(new ForumModel(forumData));
            });
        }

    } else {
        self.Id = ko.observable();
        self.Created = ko.observable();
        self.Name = ko.observable();
        self.Description = ko.observable();
        self.Order = ko.observable();
        self.IsGalleryCategory = ko.observable(false);
    }

    self.IsValid = ko.pureComputed(function () {
        if (!IsNullOrEmpty(self.Name())) {
            return true;
        }
        return false;
    }, self);

    self.CanCategoryBeDeleted = ko.pureComputed(function () {
        if (self.Forums() == null || self.Forums().length === 0) {
            return true;
        }
        return false;
    }, self);

    self.GetUrl = ko.pureComputed(function () {
        return "/forums/categories/" + self.Id() + "/" + window.EncodeUrlPart(self.Name());
    }, self);

}