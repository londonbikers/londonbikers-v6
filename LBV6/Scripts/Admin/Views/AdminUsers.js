// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel());
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function () {

    var self = this;
    self.SearchTerm = ko.observable("");
    self.TotalUsers = ko.observable();
    self.EnabledUsers = ko.observable();
    self.SuspendedUsers = ko.observable();
    self.BannedUsers = ko.observable();
    self.ConfirmedUsers = ko.observable();
    self.FacebookLogins = ko.observable();
    self.GoogleLogins = ko.observable();

    self.GetUsers = function() {
        var params = {
            limit: this.pager.limit(),
            startIndex: this.pager.limit() * (this.pager.page() - 1)
        };

        var term = self.SearchTerm();
        var encodedTerm = encodeURIComponent(term);

        $.ajax({
            type: "GET",
            url: "/api/users/SearchUsersByUsernameOrEmail?term=" + encodedTerm + "&limit=" + params.limit + "&startIndex=" + params.startIndex,
            context: this,
            success: function (data) {

                // convert the json entities to models
                var userModels = [];
                for (var i = 0; i < data.Users.length; i++) {
                    userModels.push(new UserLightExtendedModel(data.Users[i]));
                }
                this(userModels);
                this.pager.totalCount(data.TotalItems);

            },
            dataType: "json"
        });
    };

    self.OnPageChange = function () {
        self.UpdateAddress();
        $(window).scrollTop(0);
    };

    self.UpdateAddress = function () {
        var url = "/admin/users";
        if (self.Users.pager.page() > 1)
            url += "?p=" + self.Users.pager.page();
        window.ChangeUrl(url);
    };

    self.SearchForUsers = function() {
        self.Users.refresh();
    };

    self.GetName = function (firstName, lastName) {

        if (!IsNullOrEmpty(firstName) && !IsNullOrEmpty(lastName)) {
            return firstName + " " + lastName;
        } else if (!IsNullOrEmpty(firstName) && IsNullOrEmpty(lastName)) {
            return firstName;
        } else {
            return lastName;
        }

    };

    self.GetEmailConfirmedHtml = function (emailConfirmed) {
        if (emailConfirmed) {
            return "Yes";
        } else {
            return "<span class=\"light\">No</span>";
        }
    };

    self.GetPostsHtml = function (topicsCount, repliesCount) {
        var posts = topicsCount + repliesCount;
        if (posts > 0) {
            return NumberWithCommas(posts);
        } else {
            return "<span class=\"light\">-</span>";
        }
    }

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function receiveRefresh() {
        if (parseInt(self.Users.pager.page()) > 1) {
            Log("receiveRefresh: Not on the first page, not refreshing.");
            return;
        }

        self.Users.refresh();
        Log("Refreshed to show updated users");
    }

    function receiveUpdatedUser(updatedUserId) {
        Log("receiveUpdatedUser()");

        // if we have the user in view, request an updated version of the user and swap out the current one.

        // do we have the user in the view? if so, swap it out
        var currentUser = self.Users().find(function (t) {
            return parseInt(t.Id()) === parseInt(updatedUserId);
        });

        if (IsNull(currentUser)) {
            Log("Updated user not found in users list. Stopping.");
            return;
        }

        $.getJSON("/api/users/GetUser?userId=" + updatedUserId, function (userDto) {

            var updatedUser = new window.UserLightModel(userDto);
            self.Users.replace(currentUser, updatedUser);
            Log("Replaced user");

        });

    }

    function removeUser(removedUserId) {
        Log("removeUser: " + removedUserId);

        // do we have the user in the view? if so, cause a refresh to show it removed
        var currentUser = self.Users().find(function (u) {
            return parseInt(u.Id()) === parseInt(removedUserId);
        });

        if (IsNull(currentUser)) {
            Log("Removed user not found in users list. Stopping.");
            return;
        }

        self.Users.refresh();
        Log("Removed user");

    }

    function registerWithHub() {
        Log("AdminUsers: registerWithHub()");
        self.UsersHub = $.connection.usersHub;
        self.UsersHub.client.receiveRefresh = receiveRefresh;
        self.UsersHub.client.receiveUpdatedUser = receiveUpdatedUser;
        self.UsersHub.client.removeUser = removeUser;
        Log("AdminUsers: all hub proxy event bindings made");
        window.StartHub();
    }

    function init() {

        // get the latest users
        self.Users = ko.observableArray([]).extend(
        {
            datasource: self.GetUsers,
            pager: {
                limit: _defaultPageSize,
                startPage: parseInt(window.QueryString.p) || 1,
                onPageChange: self.OnPageChange
            }
        });

        // get the user stats
        $.getJSON("/api/users/GetUserStats", function (stats) {

            self.TotalUsers(stats.TotalUsers);
            self.EnabledUsers(stats.EnabledUsers);
            self.SuspendedUsers(stats.SuspendedUsers);
            self.BannedUsers(stats.BannedUsers);
            self.ConfirmedUsers(stats.ConfirmedUsers);
            self.FacebookLogins(stats.FacebookLogins);
            self.GoogleLogins(stats.GoogleLogins);

        });

        registerWithHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

};