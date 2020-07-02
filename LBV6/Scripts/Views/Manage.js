// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(window._payload), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (payload) {
    var self = this;

    self.PreferenceNewTopicNotifications = ko.observable(payload.Preferences.NewTopicNotifications);
    self.PreferenceNewReplyNotifications = ko.observable(payload.Preferences.NewReplyNotifications);
    self.PreferenceNewMessageNotifications = ko.observable(payload.Preferences.NewMessageNotifications);
    self.PreferenceNewPhotoCommentNotifications = ko.observable(payload.Preferences.NewPhotoCommentNotifications);
    self.PreferenceReceiveNewsletters = ko.observable(payload.Preferences.ReceiveNewsletters);

    // toggle element change events...
    // these are needed as Bootstrap Toggle doesn't work with Knockout (or visa-versa)
    // so we need to manually control the html elements and respond to state changes
    // and tell our Knockout observables what's going on. far from ideal!
    // --------------------------------------------------------------------------------------

    var prefNewTopicNotificationsCheckbox = $("#pref-new-topic-notifications-checkbox");
    var prefNewReplyNotificationsCheckbox = $("#pref-new-reply-notifications-checkbox");
    var prefNewMessageNotificationsCheckbox = $("#pref-new-message-notifications-checkbox");
    var prefNewPhotoCommentNotificationsCheckbox = $("#pref-new-photo-comment-notifications-checkbox");
    var prefReceiveNewslettersCheckbox = $("#pref-receive-newsletters-checkbox");

    // set initial state
    if (self.PreferenceNewTopicNotifications()) {
        prefNewTopicNotificationsCheckbox.bootstrapToggle("on");
    }

    if (self.PreferenceNewReplyNotifications()) {
        prefNewReplyNotificationsCheckbox.bootstrapToggle("on");
    }

    if (self.PreferenceNewMessageNotifications()) {
        prefNewMessageNotificationsCheckbox.bootstrapToggle("on");
    }

    if (self.PreferenceNewPhotoCommentNotifications()) {
        prefNewPhotoCommentNotificationsCheckbox.bootstrapToggle("on");
    }

    if (self.PreferenceReceiveNewsletters()) {
        prefReceiveNewslettersCheckbox.bootstrapToggle("on");
    }

    // respond to changes from the UI
    prefNewTopicNotificationsCheckbox.change(function () {
        self.PreferenceNewTopicNotifications($(this).prop("checked"));
    });

    prefNewReplyNotificationsCheckbox.change(function () {
        self.PreferenceNewReplyNotifications($(this).prop("checked"));
    });

    prefNewMessageNotificationsCheckbox.change(function () {
        self.PreferenceNewMessageNotifications($(this).prop("checked"));
    });

    prefNewPhotoCommentNotificationsCheckbox.change(function () {
        self.PreferenceNewPhotoCommentNotifications($(this).prop("checked"));
    });

    prefReceiveNewslettersCheckbox.change(function () {
        self.PreferenceReceiveNewsletters($(this).prop("checked"));
    });
    
    // respond to preference changes as normal
    // persist changes back to the server
    // just update the payload again as it's already json encoded for transfer to the server.
    // --------------------------------------------------------------------------------------
    function persistPreferenceChanges() {
        $.post("/api/users/UpdatePreferences", payload.Preferences, function () {
            window.ShowEventMessage("success", "Preferences updated");
        }).fail(function () {
            window.ShowEventMessage("error", "Oops, something went wrong, sorry");
        });
    }

    self.PreferenceNewTopicNotifications.subscribe(function (preference) {
        payload.Preferences.NewTopicNotifications = preference;
        persistPreferenceChanges();
    });

    self.PreferenceNewReplyNotifications.subscribe(function (preference) {
        payload.Preferences.NewReplyNotifications = preference;
        persistPreferenceChanges();
    });

    self.PreferenceNewMessageNotifications.subscribe(function (preference) {
        payload.Preferences.NewMessageNotifications = preference;
        persistPreferenceChanges();
    });

    self.PreferenceNewPhotoCommentNotifications.subscribe(function (preference) {
        payload.Preferences.NewPhotoCommentNotifications = preference;
        persistPreferenceChanges();
    });

    self.PreferenceReceiveNewsletters.subscribe(function (preference) {
        payload.Preferences.ReceiveNewsletters = preference;
        persistPreferenceChanges();
    });

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function init() {

        window.StartHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

};