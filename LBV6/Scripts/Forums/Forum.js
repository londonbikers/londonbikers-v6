// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    // ReSharper disable once PossiblyUnassignedProperty
    $.getJSON("/api/forums/get/" + window._forumId, function (forum) {
        ko.applyBindings(new ViewModel(forum), $("#page-binding-scope")[0]);
    });

    // get subscription status
    // this could change in the future to be info included with the model once we get the performance profile where
    // it needs to be for notificaitons for this purpose.
    if (window.IsUserLoggedIn()) {
        $("#follow-forum-div").removeClass("hide");
        // ReSharper disable once PossiblyUnassignedProperty
        $.getJSON("/api/forums/is_subscribed?id=" + window._forumId, function (isSubscribed) {
            if (isSubscribed) {
                ChangeUiToFollowingForum();
            }
        });
    }
});

// update the following button to show that the user is following the forum.
function ChangeUiToFollowingForum() {
    var btn = $("#follow-forum-btn");
    btn.text("following forum");
    btn.addClass("btn-primary");
    btn.attr("title", "Stop getting notifications");
    btn.tooltip("destroy");
    btn.tooltip();
}

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function (forum) {

    var self = this;
    self.Forum = new window.ForumModel(forum);
    self.SubscribedToForum = ko.observable();
    self.Posting = ko.observable(false);
    self.Uploading = ko.observable(false);
    self.ForumsHub = null;

    /************************************************************/

    self.GetTopics = function() {
        var params = {
            limit: this.pager.limit(),
            startIndex: this.pager.limit() * (this.pager.page() - 1)
        };
        $.ajax({
            type: "GET",
            // ReSharper disable once PossiblyUnassignedProperty
            url: "/api/topics/GetHeadersForForum?forumid=" + window._forumId + "&limit=" + params.limit + "&startIndex=" + params.startIndex,
            context: this,
            success: function (data) {
                // convert the json entities to models
                var headerModels = [];
                for (var i = 0; i < data.Headers.length; i++) {
                    headerModels.push(new window.TopicHeaderModel(data.Headers[i]));
                }
                this(headerModels);
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
        var url = self.Forum.GetUrl();
        if (self.Topics.pager.page() > 1)
            url += "?p=" + self.Topics.pager.page();
        window.ChangeUrl(url);
    };

    // subscribes or unsubscribes a user from new topic notifications
    self.ToggleSubscribeToForum = function () {
        Log("ToggleSubscribeToForum()");
        var btn = $("#follow-forum-btn");
        btn.attr("disabled", "disabled");
        if (btn.text() === "follow forum") {
            $.post("/api/forums/subscribe?id=" + self.Forum.Id(), function () {
                // success
                ChangeUiToFollowingForum();
                btn.removeAttr("disabled");
                ShowEventMessage("success", "You're now subscribed!");
            });
        } else {
            $.post("/api/forums/unsubscribe?id=" + self.Forum.Id(), function () {
                // success
                btn.removeAttr("disabled");
                btn.text("follow forum").removeClass("btn-primary").attr("title", "Get new topic notifications");
                btn.tooltip("destroy");
                btn.tooltip();
                ShowEventMessage("success", "You're now un-subscribed!");
            });
        }
    };

    self.GetTopicsTemplateName = function () {
        return "forum-topic-headers-" + window.GetDeviceType();
    };

    self.Topics = ko.observableArray([]).extend({
        datasource: self.GetTopics,
        pager: {
            limit: _defaultPageSize,
            startPage: parseInt(window.QueryString.p) || 1,
            onPageChange: self.OnPageChange
    }});

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function receiveNewTopic() {

        if (parseInt(self.Topics.pager.page()) > 1) {
            Log("receiveNewTopic: Not on the first page, not refreshing.");
            return;
        }

        self.Topics.refresh();
        Log("Refreshed to show new topic");
    }

    function receiveUpdatedTopic(updatedTopicId) {

        Log("receiveUpdatedTopic()");

        // if we have the topic in view, request an updated version of the topic header and swap out the current one.
        // we can't get this pushed to us as it's user specific and the broadcast mechanism has no user context.

        // do we have the topic in the view? if so, swap it out
        var currentTopic = self.Topics().find(function (t) {
            return parseInt(t.Id()) === parseInt(updatedTopicId);
        });

        if (IsNull(currentTopic)) {
            Log("Updated topic not found in topics list. Stopping.");
            return;
        }

        $.getJSON("/api/topics/GetHeader?topicId=" + updatedTopicId, function (topicDto) {

            var updatedHeader = new window.TopicHeaderModel(topicDto);
            self.Topics.replace(currentTopic, updatedHeader);
            self.Topics.sort(window.SortTopicHeaders);
            Log("Replaced topic");

        });

    }

    function removeTopic(removedTopicId) {
        Log("removeTopic: " + removedTopicId);

        // do we have the topic in the view? if so, swap it out
        var currentTopic = self.Topics().find(function (t) {
            return parseInt(t.Id()) === parseInt(removedTopicId);
        });

        if (IsNull(currentTopic)) {
            Log("Removed topic not found in topics list. Stopping.");
            return;
        }

        self.Topics.refresh();
        Log("Removed topic");
    }

    function registerWithHub() {

        Log("Forum.registerWithHub()");
        self.ForumsHub = $.connection.forumsHub;
        self.ForumsHub.client.receiveUpdatedTopic = receiveUpdatedTopic;
        self.ForumsHub.client.removeTopic = removeTopic;
        self.ForumsHub.client.receiveNewTopic = receiveNewTopic;
        Log("Forum: all hub proxy event bindings made");

        window._postHubStartTasks.push(function () {
            self.ForumsHub.server.viewingForum(self.Forum.Id());
            Log("Forum.viewingForum()");
        });

        window.StartHub();
    }

    function init() {
        registerWithHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

};