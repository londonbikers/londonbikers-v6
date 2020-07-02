// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function () {

    var self = this;
    self.LatestTopicsHub = null;

    self.GetTopics = function() {
        var params = {
            limit: this.pager.limit(),
            startIndex: this.pager.limit() * (this.pager.page() - 1)
        };
        $.ajax({
            type: "GET",
            url: "/api/topics/GetHeadersForLatestTopics?limit=" + params.limit + "&startIndex=" + params.startIndex,
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
        var url = "/forums";
        if (self.Topics.pager.page() > 1) {
            url += "?p=" + self.Topics.pager.page();
        }

        window.ChangeUrl(url);
    };

    self.GetTopicIconsHtml = function (topicHeader) {
        var html = "";

        if (topicHeader.IsSticky() === true) {
            html += "<span class=\"glyphicon glyphicon-pushpin light\" aria-hidden=\"true\" title=\"Pinned\"></span>";
        }

        if (topicHeader.StatusCode() === 2) {
            if (html !== "") {
                html += "<span class=\"glyphicon glyphicon-lock light ml5\" aria-hidden=\"true\" title=\"Replies locked\"></span>";
            } else {
                html += "<span class=\"glyphicon glyphicon-lock light\" aria-hidden=\"true\" title=\"Replies locked\"></span>";
            }
        }
        return html;
    };

    self.GetTopicsTemplateName = function () {
        var name = "latest-topic-headers-" + window.GetDeviceType();
        return name;
    };

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
            self.Topics.sort(window.SortTopicHeadersByDate);
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

        Log("LatestTopics: registerWithHub()");
        self.LatestTopicsHub = $.connection.latestTopicsHub;
        self.LatestTopicsHub.client.receiveNewTopic = receiveNewTopic;
        self.LatestTopicsHub.client.receiveUpdatedTopic = receiveUpdatedTopic;
        self.LatestTopicsHub.client.removeTopic = removeTopic;
        Log("LatestTopics: all hub proxy event bindings made");

        window._postHubStartTasks.push(function () {
            self.LatestTopicsHub.server.viewingLatestTopics();
            Log("LatestTopics: viewingLatestTopics()");
        });

        window.StartHub();
    }

    function init() {

        self.Topics = ko.observableArray([]).extend(
        {
            datasource: self.GetTopics,
            pager: {
                limit: _defaultPageSize,
                startPage: parseInt(window.QueryString.p) || 1,
                onPageChange: self.OnPageChange
            }
        });

        registerWithHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

};