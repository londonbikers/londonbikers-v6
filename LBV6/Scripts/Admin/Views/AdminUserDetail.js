// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(window._payload));
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (profile) {

    var self = this;
    self.Profile = ko.observable(new window.UserExtendedModel(profile));
    self.SavingProfileChanges = ko.observable(false);
    self.SavingStatusChange = ko.observable(false);
    self.SavingUsernameChange = ko.observable(false);
    self.SavingEmailChange = ko.observable(false);
    self.RemovingProfilePhoto = ko.observable(false);
    self.RemovingCoverPhoto = ko.observable(false);
    
    // set initial checkbox state
    var verifiedCheckbox = $("#verified-checkbox");
    if (self.Profile().Verified()) {
        verifiedCheckbox.bootstrapToggle("on");
    }

    // respond to checkbox changes from the UI
    verifiedCheckbox.change(function () {
        self.Profile().Verified($(this).prop("checked"));
    });

    function persistVerifiedChange() {
        $.post("/api/users/UpdateVerifiedState?userId=" + self.Profile().Id + "&verified="+ self.Profile().Verified()).done(function () {
            ShowEventMessage("success", "<b>Verified State Updated</b>");
        }).fail(function () {
            window.ShowEventMessage("error", "Oops, something went wrong, sorry");
        });
    }

    self.Profile().Verified.subscribe(function () {
        persistVerifiedChange();
    });

    self.ChangeStatus = function (newStatusNumber) {

        self.Profile().Status(newStatusNumber);
        self.SavingStatusChange(true);

        $.post("/api/users/UpdateStatus?userId=" + self.Profile().Id + "&status=" + newStatusNumber).done(function() {
            window.ShowEventMessage("success", "<b>Status Changed</b>");
        }).fail(function() {
            ShowEventMessage("error", window._notificationGeneralErrorText);
        }).always(function () {
            self.SavingStatusChange(false);
        });

    };

    self.ChangeUsername = function () {

        Log("ChangeUsername()");
        self.SavingUsernameChange(true);

        $.post("/api/users/UpdateUsername?userId=" + self.Profile().Id + "&username=" + encodeURIComponent(self.Profile().UserName())).done(function () {
            window.ShowEventMessage("success", "<b>Username Changed</b>");
        }).fail(function (error) {
            ShowEventMessage("error", error.responseJSON.Message);
        }).always(function () {
            self.SavingUsernameChange(false);
        });

    };

    self.ChangeEmail = function() {
        
        Log("ChangeEmail()");
        self.SavingEmailChange(true);

        $.post("/api/users/UpdateEmailAddress?userId=" + self.Profile().Id + "&emailAddress=" + encodeURIComponent(self.Profile().Email())).done(function () {
            window.ShowEventMessage("success", "<b>Email Changed</b>");
        }).fail(function (error) {
            ShowEventMessage("error", error.responseJSON.Message);
        }).always(function () {
            self.SavingEmailChange(false);
        });

    };

    self.SaveAttributeChanges = function () {

        Log("SaveAttributeChanges()");

        // strip html from the legacy site biography values first so we're working with plain-text
        self.Profile().Biography(window.HtmlToText(self.Profile().Biography()));

        var jsonModel = ko.toJSON(self.Profile());
        self.SavingProfileChanges(true);
        $.ajax({
            url: "/api/users/UpdateProfileExtended",
            type: "POST",
            data: jsonModel,
            contentType: "application/json;charset=utf-8",
            success: function () {
                window.ShowEventMessage("success", "<b>Updated profile</b>");
                self.SavingProfileChanges(false);
            },
            error: function () {
                ShowEventMessage("error", window._notificationGeneralErrorText);
                self.SavingProfileChanges(false);
            }
        });

    };

    self.RemoveProfilePhoto = function () {

        self.RemovingProfilePhoto(true);
        $.post("/api/users/RemoveProfilePhoto?userId=" + self.Profile().Id).done(function () {
            window.ShowEventMessage("success", "<b>Profile Photo Removed</b>");
            self.Profile().ProfilePhoto(null);
        }).fail(function () {
            ShowEventMessage("error", window._notificationGeneralErrorText);
        }).always(function () {
            self.RemovingProfilePhoto(false);
        });

    };

    self.RemoveCoverPhoto = function () {

        self.RemovingCoverPhoto(true);
        $.post("/api/users/RemoveProfileCover?userId=" + self.Profile().Id).done(function () {
            window.ShowEventMessage("success", "<b>Profile Cover Removed</b>");
            self.Profile().CoverFileStoreId(null);
        }).fail(function () {
            ShowEventMessage("error", window._notificationGeneralErrorText);
        }).always(function () {
            self.RemovingCoverPhoto(false);
        });

    };
    
    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function init() {

        
    }

    // --[ INIT ]---------------------------------------------------------------

    init();
};