function PrivateMessageHeaderModel(data) {
    var self = this;
    self.Users = ko.observableArray();
    self.LastMessageCreated = ko.observable();
    self.HasUnreadMessagesForCurrentUser = ko.observable(false);

    if (data != undefined) {
        self.Id = data.Id;
        self.Created = data.Created;
        self.LastMessageCreated(new Date(data.LastMessageCreated));
        self.HasUnreadMessagesForCurrentUser(data.HasUnreadMessagesForCurrentUser);

        // convert the json users to models first
        for (var i = 0; i < data.Users.length; i++) {
            self.Users.push(new window.PrivateMessageHeaderUserModel(data.Users[i]));
        }
    } else {
        self.Id = null;
        self.Created = new Date();
    }

    self.IsValid = ko.pureComputed(function () {

        var currentUserIn = false;
        var otherUserIn = false;

        // make sure the current user is in the header users
        for (var x = 0; x < self.Users().length; x++) {
            if (self.Users()[x].User.Id === window._user.Id) {
                currentUserIn = true;
            }
            if (self.Users()[x].User.Id !== window._user.Id) {
                otherUserIn = true;
            }
            if (currentUserIn && otherUserIn) {
                break;
            }
        }

        if (!currentUserIn || !otherUserIn) {
            return false;
        }

        if (self.Users().length > parseInt(window._maxHeaderUserCount)) {
            return false;
        }

        // all seems good!
        return true;
    }, self);

    self.GetRelativeLastMessageCreated = ko.pureComputed(function () {

        if (!IsNull(self.LastMessageCreated())) {
            return window.moment(self.LastMessageCreated()).fromNow();
        } else {
            // no last message created value. this can happen in edge cases
            return "...";
        }
        
    }, self);

    // returns the first user from the header who isn't the current user.
    // many messages will just be to a single person, so this is to make it easy to show those types of headers.
    self.FirstNonCurrentUser = ko.pureComputed(function () {
        for (var x = 0; x < self.Users().length; x++) {
            if (self.Users()[x].User.Id !== window._user.Id) {
                return self.Users()[x].User;
            }
        }
        // there should always be at least one other user in the pm user list
        return null;
    });

    self.GetUserListCsv = ko.pureComputed(function() {
        var csv = "";

        for (var x = 0; x < self.Users().length; x++) {
            csv += self.Users()[x].User.UserName;
            if (x < self.Users().length - 1) {
                csv += ", ";
            }
        }

        return csv;

    }, self);
}