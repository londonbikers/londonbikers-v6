// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function () {
    var self = this;
    self.DaysToShow = ko.observable(1);
    self.PopularTopicsHub = null;

    self.GetTopics = function() {
        var params = {
            limit: this.pager.limit(),
            startIndex: this.pager.limit() * (this.pager.page() - 1)
        };
        Log("GetTopics()");
        $.ajax({
            type: "GET",
            url: "/api/topics/GetHeadersForPopularTopics?days=" + self.DaysToShow() + "&limit=" + params.limit + "&startIndex=" + params.startIndex,
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

    self.ChangeDaysToShow = function (newDaysToShow) {
        Log("ChangeDaysToShow()");
        self.DaysToShow(newDaysToShow);

        // re-initialise the topics
        self.Topics = ko.observableArray([]).extend(
        {
            datasource: self.GetTopics,
            pager: {
                limit: 25,
                startPage: 1,
                onPageChange: self.OnPageChange
            }
        });

        // save preference to a cookie
        $.cookie("popular_topics_day_preference", self.DaysToShow().toString(), { expires: 9999 });

        // change the button styling
        self.SetDaysToShowButtonStyle();
    };

    self.SetDaysToShowButtonStyle = function() {
        if (self.DaysToShow() === 1) {
            $("#btn-day-1").removeClass("btn-primary").removeClass("btn-default").addClass("btn-primary");
            $("#btn-day-7").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-30").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-90").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
        } else if (self.DaysToShow() === 7) {
            $("#btn-day-1").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-7").removeClass("btn-primary").removeClass("btn-default").addClass("btn-primary");
            $("#btn-day-30").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-90").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
        } else if (self.DaysToShow() === 30) {
            $("#btn-day-1").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-7").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-30").removeClass("btn-primary").removeClass("btn-default").addClass("btn-primary");
            $("#btn-day-90").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
        } else if (self.DaysToShow() === 90) {
            $("#btn-day-1").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-7").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-30").removeClass("btn-primary").removeClass("btn-default").addClass("btn-default");
            $("#btn-day-90").removeClass("btn-primary").removeClass("btn-default").addClass("btn-primary");
        }
    };

    self.OnPageChange = function () {
        self.UpdateAddress();
        $(window).scrollTop(0);
    };

    self.UpdateAddress = function () {
        var url = "/forums/popular";
        if (self.Topics.pager.page() > 1)
            url += "?p=" + self.Topics.pager.page();
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
        // we can re-use the latest view template for now as they're the same.
        return "latest-topic-headers-" + window.GetDeviceType();
    };

    // does the user have a prference for how many days to show?
    var daysPref = $.cookie("popular_topics_day_preference");
    if (!IsNull(daysPref)) {
        self.DaysToShow(parseInt(daysPref));
    }

    self.Topics = ko.observableArray([]).extend(
    {
        datasource: self.GetTopics,
        pager: {
            limit: _defaultPageSize,
            startPage: parseInt(window.QueryString.p) || 1,
            onPageChange: self.OnPageChange
        }
    });

    // set the initial state of the days to show buttons:
    self.SetDaysToShowButtonStyle();

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------
    
    function refresh() {

        if (parseInt(self.Topics.pager.page()) > 1) {
            Log("PopularTopics.refresh: Not on the first page, not refreshing.");
            return;
        }

        self.Topics.refresh();
        Log("PopularTopics.refresh: Refreshed");
    }
    
    function receiveUpdatedTopic(updatedTopicId) {

        Log("PopularTopics.receiveUpdatedTopic");

        // cheating here, just going to refresh so we don't have to implement the ranking algorithm
        refresh();

    }
    
    function removeTopic(removedTopicId) {

        Log("PopularTopics.removeTopic: " + removedTopicId);

        // do we have the topic in the view? if so, swap it out
        var currentTopic = self.Topics().find(function (t) {
            return parseInt(t.Id()) === parseInt(removedTopicId);
        });

        if (IsNull(currentTopic)) {
            Log("PopularTopics.removeTopic: Removed topic not found in topics list. Stopping.");
            return;
        }

        self.Topics.refresh();
        Log("PopularTopics.removeTopic: Removed topic");

    }
    
    function registerWithHub() {

        Log("PopularTopics.registerWithHub()");
        self.PopularTopicsHub = $.connection.popularTopicsHub;
        self.PopularTopicsHub.client.refresh = refresh;
        self.PopularTopicsHub.client.receiveUpdatedTopic = receiveUpdatedTopic;
        self.PopularTopicsHub.client.removeTopic = removeTopic;
        Log("PopularTopics.registerWithHub: all hub proxy event bindings made");

        window._postHubStartTasks.push(function () {
            self.PopularTopicsHub.server.viewingPopularTopics();
            Log("PopularTopics.registerWithHub: viewingPopularTopics");
        });

        window.StartHub();
    }

    function init() {
        registerWithHub();
    }

    // --[ INIT ]---------------------------------------------------------------

    init();

};