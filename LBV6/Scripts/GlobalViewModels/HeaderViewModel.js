var HeaderViewModel = function () {

    var self = this;
    self.Notifications = ko.observableArray();
    self.NotificationsHub = null;
    self.NewNotificationsCount = ko.observable();
    self.IntercomHub = null;
    self.UnreadMessagesCount = ko.observable();
    self.ShowNotifications = ko.observable(false);
    
    // --[ VIEW FUNCTIONS ]----------------------------------------------

    self.ShowNotificationMenu = function () {
        Log("ShowNotificationMenu()");

        // is the menu already open? in which case, close it, so act as a toggle
        if (self.ShowNotifications()) {
            self.HideNotificationMenu();
            return;
        }

        self.ShowNotifications(true);

        // disable notifications button tooltip whilst showing the menu
        $("#btn-notifications").tooltip("destroy");

        // wire up the escape key to close the menu
        $(document).keyup(function (e) {
            if (e.keyCode === 27) {
                // escape key maps to key-code '27'
                self.HideNotificationMenu();
            }
        });

        $("a").css("cursor", "default");
        $("button").css("cursor", "default");
    }

    self.HideNotificationMenu = function() {
        Log("HideNotificationMenu()");
        self.ShowNotifications(false);

        // enable the notifications button tooltip again
        $("#btn-notifications").tooltip();

        $("a").css("cursor", "auto");
        $("button").css("cursor", "pointer");
    }

    self.ClearAllNotifications = function() {
        Log("Clear all notifications...");
        $.ajax({
            url: "/api/notifications/ClearAll",
            type: "DELETE",
            success: function (result) {
                Log("Cleared all notifications.");

                // remove the templated notification items
                self.Notifications.removeAll();

                // clear the notification count icon
                self.NewNotificationsCount(0);
            }
        });
    }

    // --[ PRIVATE FUNCTIONS ]-------------------------------------------

    function receiveNotifications(jsonNotifications) {
        Log("receiveNotifications()");

        for (var i = 0; i < jsonNotifications.length; i++) {
            self.Notifications.push(new window.NotificationModel(jsonNotifications[i]));
        }

        self.NewNotificationsCount(self.Notifications().length);
    }

    function init() {
        Log("HeaderViewModel.init()");

        // use JavaScript to change the top-padding being applied to the mobile nav-bar. hacky, I know
        // but CSS doesn't support operating system detection!
        // http://stackoverflow.com/questions/8493589/is-there-a-css-media-query-to-detect-windows

        if (GetOperatingSystem() === "Windows") {
            $("#header-mobile-nav-row").addClass("header-mobile-nav-row-win");
        }

        if (!IsUserLoggedIn()) {
            return;
        }
        
        self.NotificationsHub = $.connection.generalNotificationsHub;
        self.NotificationsHub.client.receiveNotifications = receiveNotifications;
        self.UnreadMessagesCount(window._user.UnreadMessagesCount);
        self.IntercomHub = $.connection.intercomNotificationsHub;

        // this needs to run only when the hub has been started
        window._postHubStartTasks.push(function () {
            // get the initial notifications
            self.NotificationsHub.server.getNotifications().done(receiveNotifications);
        });

        self.IntercomHub.client.updateUnreadMessageCount = function (unreadMessageCount) {
            Log("updateUnreadMessageCount(): " + unreadMessageCount);

            var newMessages = self.UnreadMessagesCount() < unreadMessageCount;
            self.UnreadMessagesCount(unreadMessageCount);

            if (newMessages) {
                new Audio("/content/audio/new-message.mp3").play();
            }
        };

        // handles closing the menu when it's open by the user clicking anywhere but the menu
        $(document.body).click(function (e) {

            // is the target under the notifications-menu element? if so, don't do anything
            if (self.ShowNotifications() &&
                (
                    e.target.id === "notifications-menu" ||
                    window.isDescendant(document.getElementById("notifications-menu"), e.target) ||

                    e.target.id === "btn-notifications" ||
                    window.isDescendant(document.getElementById("btn-notifications"), e.target)

                )) {
                return true;

            } else if (self.ShowNotifications()) {

                Log("Click outside of menu - closing.");
                self.HideNotificationMenu();

                // return false to disable any links clicked on whilst trying to close the menu
                return false;

            }

            return true;

        });
    }

    // --[ INIT ]--------------------------------------------------------

    init();

};