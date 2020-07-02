// ---[ MAIN ]------------------------------------------------------------------------------
$(document).ready(function () {
    $.getJSON("/api/categories", function (categories) {
        ko.applyBindings(new ViewModel(categories), $("#page-binding-scope")[0]);
    });
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (categories) {

    var self = this;
    self.Categories = ko.observableArray();
    self.ForumsHomepageHub = null;
    self.LatestTopicsHub = null;

    self.GetStructureTemplateName = function () {
        return "forum-structure-" + window.GetDeviceType();
    };

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function receiveUpdatedForum(updatedForumDto) {

        Log("receiveUpdatedForum()");

        var updatedForum = new window.ForumModel(updatedForumDto);

        // do we have the forum in the view? if so, swap it out
        var category = self.Categories().find(function (c) {
            return parseInt(c.Id()) === parseInt(updatedForum.CategoryId());
        });

        if (IsNull(category)) {
            Log("Forum category not found. Stopping.");
            return;
        }

        var currentForum = category.Forums().find(function(f) {
            return parseInt(f.Id()) === parseInt(updatedForum.Id());
        });

        if (IsNull(currentForum)) {
            Log("Updated forum not found in forums list. Stopping.");
            return;
        }

        // replace the forum
        category.Forums.replace(currentForum, updatedForum);
        Log("Replaced forum");
    }

    function registerWithHub() {

        Log("Index.registerWithHub()");
        // ReSharper disable once PossiblyUnassignedProperty
        self.ForumsHomepageHub = $.connection.forumsHomepageHub;
        // ReSharper disable once PossiblyUnassignedProperty
        self.ForumsHomepageHub.client.receiveUpdatedForum = receiveUpdatedForum;
        Log("Index: all hub proxy event bindings made");

        window._postHubStartTasks.push(function () {
            // ReSharper disable once PossiblyUnassignedProperty
            self.ForumsHomepageHub.server.viewingForumsHomepage();
            Log("Index.viewingForumsHomepage()");
        });

        window.StartHub();
    }
    
    function init() {

        registerWithHub();

        // convert the json categories and forums to models
        for (var i = 0; i < categories.length; i++) {
            var categoryModel = new window.CategoryModel(categories[i]);
            for (var k = 0; k < categoryModel.Forums.length; k++) {
                categoryModel.Forums.push(new window.ForumModel(categories.Forums[k]));
            }
            self.Categories.push(categoryModel);
        }
    }

    // --[ INIT ]---------------------------------------------------------------

    init();
};