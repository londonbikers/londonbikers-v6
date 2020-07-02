function PostModerationHistoryItem(data) {
    var self = this;

    if (data != undefined) {
        self.ModeratorId = ko.observable(data.ModeratorId);
        self.ModeratorUserName = ko.observable(data.ModeratorUserName);
        self.Type = ko.observable(data.Type);
        self.Reason = ko.observable(data.Reason);
        self.Created = ko.observable(data.Created);
    } else {
        self.ModeratorId = ko.observable();
        self.ModeratorUserName = ko.observable();
        self.Type = ko.observable();
        self.Reason = ko.observable();
        self.Created = ko.observable();
    }
}