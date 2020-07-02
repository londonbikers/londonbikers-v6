// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    $.getJSON("/api/categories?extended=true", function (categories) {
        var viewModel = new ViewModel(categories);

        // control visibility, give element focus, and select the contents (in order)
        ko.bindingHandlers.visibleAndSelect = {
            update: function (element, valueAccessor) {
                ko.bindingHandlers.visible.update(element, valueAccessor);
                if (valueAccessor()) {
                    setTimeout(function () {
                        $(element).find("input").focus().select();
                    }, 0); //new tasks are not in DOM yet
                }
            }
        };
        ko.applyBindings(viewModel);
    });
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function(categories) {
    var self = this;

    self.Categories = ko.observableArray();
    self.SelectedCategory = ko.observable();
    self.SelectedForum = ko.observable();
    self.NewCategory = ko.observable(new CategoryModel());
    self.NewForumAccessRole = ko.observable(new ForumAccessRoleModel());
    self.NewForumPostRole = ko.observable(new ForumPostRoleModel());

    // ---[ CATEGORIES ]----------------------------------------

    // new category functionality
    self.CreateCategory = function() {
        var jsonNewCategory = ko.toJSON(self.NewCategory());
        $.ajax({
            url: "/api/categories",
            type: "POST",
            data: jsonNewCategory,
            contentType: "application/json;charset=utf-8",
            success: function (persistedCategory) {
                ShowEventMessage("success", self.NewCategory().Name() + " category created!");
                self.NewCategory().Id(persistedCategory.Id);
                self.NewCategory().NewForum().CategoryId(persistedCategory.Id);
                // add new category to our view list
                self.Categories.push(self.NewCategory());
                // order the categories so the new one is last
                self.OrderCategories();
                // reset form
                self.NewCategory(new window.CategoryModel());
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    }

    // order category functionality
    self.OrderCategories = function () {
        Log("ordering categories...");
        for (var x = 0; x < self.Categories().length; x = x + 1) {
            self.Categories()[x].Order = x;
            var jsonCategory = ko.toJSON(self.Categories()[x]);
            $.ajax({
                url: "/api/categories",
                data: jsonCategory,
                type: "PUT",
                contentType: "application/json;charset=utf-8",
                success: function () {
                    Log("Re-ordered a category.");
                }, error: function (error) {
                    Log(error);
                }
            });
        }
    }

    // edit category functionality
    self.RenameCategory = function (category) {
        if (!category.IsValid()) {
            ShowEventMessage("error", "Please supply a name for the category.");
            return;
        }

        var jsonCategory = ko.toJSON(category);
        $.ajax({
            url: "/api/categories",
            data: jsonCategory,
            contentType: "application/json;charset=utf-8",
            type: "PUT",
            success: function () {
                ShowEventMessage("success", "Saved changes to " + category.Name());
            }, error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
        
        if (category === self.SelectedCategory()) {
            self.SelectedCategory(null);
        }
    };

    self.IsCategorySelected = function (category) {
        return category === self.SelectedCategory();
    }

    // delete category functionality
    self.DeleteCategory = function (category) {
        $.ajax({
            url: "/api/categories/" + category.Id(),
            type: "DELETE",
            success: function () {
                ShowEventMessage("success", "Deleted the " + category.Name() + " category!");
                self.Categories.remove(function(item) {
                    return item.Id() === category.Id();
                });
            }, error: function () {
                ShowEventMessage("error", "Couldn't delete the " + category.Name() + " category!");
            }
        });
    }

    // ---[ FORUMS ]--------------------------------------------

    self.SelectForum = function(forum) {
        self.SelectedForum(forum);

        var forumAccessRole = new window.ForumAccessRoleModel();
        forumAccessRole.ForumId(forum.Id());
        self.NewForumAccessRole(forumAccessRole);

        var forumPostRole = new window.ForumPostRoleModel();
        forumPostRole.ForumId(forum.Id());
        self.NewForumPostRole(forumPostRole);
    };

    /**
     * Loses the marker on the currently-selected forum
     * which causes the forum pane to close
     * @returns {}
     */
    self.DeselectForum = function() {
        self.SelectedForum(null);
    }

    // new forum functionality
    self.CreateForum = function (category) {
        category.NewForum().CategoryId(category.Id());
        var jsonNewForum = ko.toJSON(category.NewForum());
        $.ajax({
            url: "/api/forums",
            type: "POST",
            data: jsonNewForum,
            contentType: "application/json;charset=utf-8",
            success: function (persistedForum) {
                ShowEventMessage("success", category.NewForum().Name() + " forum created!");
                category.NewForum().Id(persistedForum.Id);
                // add new forum to our view list
                category.Forums.push(category.NewForum());
                // reset form
                category.NewForum(new window.ForumModel());
                // order the forums so the new one is last
                self.OrderForums(category);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    }

    // order forums functionality
    self.OrderForums = function (args) {
        Log("ordering forums...");

        // get the category
        var category;
        for (var j = 0; j < self.Categories().length; j++) {
            if (self.Categories()[j].Id() === args.item.CategoryId()) {
                category = self.Categories()[j];
                break;
            }
        }

        if (category == null) {
            ShowEventMessage("error", window._notificationGeneralErrorText);
            return;
        }

        for (var y = 0; y < category.Forums().length; y++) {
            category.Forums()[y].Order = y;
            var jsonForum = ko.toJSON(category.Forums()[y]);
            $.ajax({
                url: "/api/forums",
                data: jsonForum,
                type: "PUT",
                contentType: "application/json;charset=utf-8",
                success: function () {
                    Log("Re-ordered a forum.");
                }, error: function (error) {
                    Log(error);
                }
            });
        }
    }

    // edit forum functionality
    self.RenameForum = function () {

        Log("Renaming forum...");
        if (!self.SelectedForum().IsValid()) {
            ShowEventMessage("error", "Please supply a name for the forum.");
            return;
        }

        var jsonForum = ko.toJSON(self.SelectedForum());
        $.ajax({
            url: "/api/forums",
            data: jsonForum,
            contentType: "application/json;charset=utf-8",
            type: "PUT",
            success: function () {
                ShowEventMessage("success", "Saved changes to " + self.SelectedForum().Name());
            }, error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });

    };

    self.IsForumSelected = function (forum) {
        return forum === self.SelectedForum();
    }

    // delete forum functionality
    self.DeleteForum = function (forum) {
        $.ajax({
            url: "/api/forums/" + forum.Id(),
            type: "DELETE",
            success: function () {
                ShowEventMessage("success", "Deleted the " + forum.Name() + " forum!");
                Log("deleted forum");

                // get the category
                var category;
                for (var j = 0; j < self.Categories().length; j++) {
                    if (self.Categories()[j].Id() === forum.CategoryId()) {
                        category = self.Categories()[j];
                        break;
                    }
                }
                if (category == null) {
                    Log("no category found!");
                    ShowEventMessage("error", window._notificationGeneralErrorText);
                    return;
                }
                category.Forums.remove(function (item) {
                    return item.Id() === forum.Id();
                });


            }, error: function () {
                ShowEventMessage("error", "Couldn't delete the " + forum.Name() + " forum!");
            }
        });
    }

    /**
     * Persists a new Forum Access Role to the currently selected forum.
     * @returns {} 
     */
    self.AddForumAccessRole = function() {
        Log("AddForumAccessRole()");

        if (IsNullOrEmpty(self.NewForumAccessRole().Role())) {
            Log("No role supplied.");
            return;
        }

        var jsonNewForumAccessRole = ko.toJSON(self.NewForumAccessRole());
        $.ajax({
            url: "/api/forums/AddForumAccessRole",
            type: "POST",
            data: jsonNewForumAccessRole,
            contentType: "application/json;charset=utf-8",
            success: function (persistedForumAccessRole) {

                Log(persistedForumAccessRole);

                ShowEventMessage("success", self.NewForumAccessRole().Role() + " Forum Access Role created!");
                self.SelectedForum().AccessRoles.push(new ForumAccessRoleModel(persistedForumAccessRole));

                // reset form
                self.NewForumAccessRole(new ForumAccessRoleModel());

            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    };

    /**
     * Causes a specific Forum Access Role to be removed from the currently selected forum.
     * @returns {} 
     */
    self.RemoveAccessRole = function(forumAccessRole) {
        Log("RemoveAccessRole()");

        if (forumAccessRole === undefined) {
            Log("No role supplied.");
            return;
        }

        var jsonForumAccessRole = ko.toJSON(forumAccessRole);
        $.ajax({
            url: "/api/forums/RemoveForumAccessRole",
            type: "DELETE",
            data: jsonForumAccessRole,
            contentType: "application/json;charset=utf-8",
            success: function () {
                ShowEventMessage("success", forumAccessRole.Role() + " Forum Access Role removed!");
                self.SelectedForum().AccessRoles.remove(function(role) { return role.Id() === forumAccessRole.Id(); });
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    };

    /**
     * Persists a new Forum Post Role to the currently selected forum.
     * @returns {} 
     */
    self.AddForumPostRole = function () {
        Log("AddForumPostRole()");

        if (IsNullOrEmpty(self.NewForumPostRole().Role())) {
            Log("No role supplied.");
            return;
        }

        var jsonNewForumPostRole = ko.toJSON(self.NewForumPostRole());
        $.ajax({
            url: "/api/forums/AddForumPostRole",
            type: "POST",
            data: jsonNewForumPostRole,
            contentType: "application/json;charset=utf-8",
            success: function (persistedForumPostRole) {

                ShowEventMessage("success", self.NewForumPostRole().Role() + " Forum Post Role created!");
                self.SelectedForum().PostRoles.push(new ForumPostRoleModel(persistedForumPostRole));

                // reset form
                self.NewForumPostRole(new ForumPostRoleModel());

            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    };

    /**
     * Causes a specific Forum Post Role to be removed from the currently selected forum.
     * @returns {} 
     */
    self.RemovePostRole = function (forumPostRole) {
        Log("RemovePostRole()");

        if (forumPostRole === undefined) {
            Log("No role supplied.");
            return;
        }

        var jsonForumPostRole = ko.toJSON(forumPostRole);
        $.ajax({
            url: "/api/forums/RemoveForumPostRole",
            type: "DELETE",
            data: jsonForumPostRole,
            contentType: "application/json;charset=utf-8",
            success: function () {
                ShowEventMessage("success", forumPostRole.Role() + " Forum Post Role removed!");
                self.SelectedForum().PostRoles.remove(function (role) { return role.Id() === forumPostRole.Id(); });
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
            }
        });
    };

    // ---[ INIT ]----------------------------------------------

    function init() {

        // convert the json categories and forums to proper models
        categories.forEach(function (categoryData) {
            self.Categories.push(new CategoryModel(categoryData, ForumExtendedModel));
        });

    }

    init();
};