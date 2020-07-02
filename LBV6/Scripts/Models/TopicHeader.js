function TopicHeaderModel(data) {
    var self = this;

    if (data != undefined) {
        self.Id = ko.observable(data.Id);
        self.Updated = ko.observable(new Date(data.Updated));
        self.Subject = ko.observable(data.Subject);
        self.ForumId = ko.observable(data.ForumId);
        self.ForumName = ko.observable(data.ForumName);
        self.IsSticky = ko.observable(data.IsSticky);
        self.UpVotes = ko.observable(data.UpVotes);
        self.DownVotes = ko.observable(data.DownVotes);
        self.ReplyCount = ko.observable(data.ReplyCount);
        self.StatusCode = ko.observable(data.StatusCode);
        self.IsNew = ko.observable(data.IsNew);
        self.HasNewReplies = ko.observable(data.HasNewReplies);
        self.FirstUnreadReplyId = ko.observable(data.FirstUnreadReplyId);
        self.FirstUnreadReplyPosition = ko.observable(data.FirstUnreadReplyPosition);
        self.HasAttachments = ko.observable(data.HasAttachments);
        self.HasPhotos = ko.observable(data.HasPhotos);

        self.ProminentUsers = ko.observableArray();
        data.ProminentUsers.forEach(function (prominentUser) {
            self.ProminentUsers.push(new ProminentUserModel(prominentUser));
        });
    }

    self.GetUrl = ko.pureComputed(function () {

        var baselineUrl = "/forums/posts/" + self.Id() + "/" + window.EncodeUrlPart(self.Subject());

        if (self.IsNew()) {

            return baselineUrl;

        } else if (self.HasNewReplies()) {

            // what page does the reply index lay on?
            // too stupid to work out a way to do this more succinctly.
            var page = 1;
            var pageItemCount = 1;
            for (var i = 0; i < self.FirstUnreadReplyPosition() ; i++) {
                if (pageItemCount === _defaultPageSize) {
                    page++;
                    pageItemCount = 1;
                    continue;
                }
                pageItemCount++;
            }

            var url1 = baselineUrl;
            if (page > 1) {
                url1 += "?p=" + page + "&hid=" + self.FirstUnreadReplyId();
            } else {
                url1 += "?hid=" + self.FirstUnreadReplyId();
            }

            return url1;

        } else {

            // user has read everything - link to last page
            var lastPageDouble = self.ReplyCount() / _defaultPageSize;
            var lastPage = Math.ceil(lastPageDouble);
            var url2 = baselineUrl;
            if (lastPageDouble >= 1) {
                url2 += "?p=" + lastPage;
            }
            return url2;
        }

    }, self);

    self.GetForumUrl = ko.pureComputed(function () {
        return "/forums/" + self.ForumId() + "/" + window.EncodeUrlPart(self.ForumName());
    }, self);

    self.GetFriendlyUpdated = ko.pureComputed(function () {
        // this should show a time like:
        // 24 mins go
        // if date > 7 days then short date format
        var now = new Date();
        var dateDiff = window.DateDiff(now, self.Updated());
        if (dateDiff <= 7) {
            return window.moment(self.Updated()).fromNow();
        } else {
            return window.moment(self.Updated()).format("ll");
        }
    }, self);

    self.GetShortFriendlyUpdated = ko.pureComputed(function () {
        return window.moment(self.Updated()).fromNow();
    }, self);

    self.GetFriendlyReplyCount = ko.pureComputed(function () {
        if (self.ReplyCount() > 0) {
            return window.NumberWithCommas(self.ReplyCount());
        } else {
            return "";
        }
    }, self);
}