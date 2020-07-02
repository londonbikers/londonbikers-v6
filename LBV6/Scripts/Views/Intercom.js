// ---[ MAIN ]------------------------------------------------------------------------------

$(document).ready(function () {
    ko.applyBindings(new ViewModel(), $("#page-binding-scope")[0]);
});

// ---[ VIEW MODEL ]------------------------------------------------------------------------

var ViewModel = function () {

    // --[ MEMBERS ]------------------------------------------------------------

    var self = this;

    self.ActionedShowHeaderByUri = ko.observable(false);
    self.ActionedShowMessageByUri = ko.observable(false);
    self.CurrentHeaderUpdateReadStatusTimer = null;
    self.CurrentPrivateMessageHeader = ko.observable();
    self.Headers = ko.observableArray();
    self.Headers.extend({ infinitescroll: {} });
    self.HeadersFilterUnread = ko.observable(false);
    self.HeadersBeingRetrieved = ko.observable(false);
    self.Messages = ko.observableArray();
    self.Messages.extend({ infinitescroll: {} });
    self.NewPrivateMessage = ko.observable();
    self.ProcessingMessageUserDirect = ko.observable(false);
    self.ProcessingSelfRemoval = ko.observable(false);
    self.ProcessingSendMessage = ko.observable(false);
    self.SelectedHeaderElement = null;
    self.ShowingHeader = ko.observable(false);
    self.ShowingHeaderLoader = ko.observable(false);
    self.ShowingUserBox = ko.observable(false);
    self.ShowingUserSearchLoader = ko.observable(false);
    self.ShowingUserSearchNoResults = ko.observable(false);
    self.UserBeingRemoved = ko.observable();
    self.UserSearchResults = ko.observableArray();
    self.UserSearchTerm = ko.observable();
    self.LoaderTimout = null;
    self.IntercomHub = null;

    // --[ PRIVATE FUNCTIONS ]--------------------------------------------------

    function executeRemoveSelfFromHeader() {
        self.ProcessingSelfRemoval(true);
        $.post("/api/intercom/RemoveMesageHeaderUser?headerId=" + self.CurrentPrivateMessageHeader().Id + "&userId=" + window._user.Id, function () {

            self.ProcessingSelfRemoval(false);

            // copy the header id as we'll lose the current header value shortly
            var headerId = self.CurrentPrivateMessageHeader().Id;

            // close the current message
            self.CloseMessage();

            // remove the header from the headers list
            self.Headers.remove(function (header) { return header.Id === headerId; });

        }).fail(function () {
            window.ShowEventMessage("error", _notificationGeneralErrorText);
            self.ProcessingSelfRemoval(false);
        });
    }

    // when a message has been marked as read, we need to update the model so that we don't end up trying to mark it as read again.
    function addMessageReadBy(messageId) {
        var message = _.find(self.Messages(), function (m) { return m.Id === messageId });

        if (IsNull(message)) {
            Log("addMessageReadBy(): No such message: " + messageId);
            return;
        }

        if (!message.IsRead()) {
            // a new read-by
            var readBy = new ReadByModel();
            readBy.UserId = window._user.Id;
            readBy.UserName = window._user.UserName;
            readBy.When = new Date();
            message.ReadBy.push(readBy);
        }
    }

    function initialiseNewPrivateMessage() {
        self.NewPrivateMessage(new window.PrivateMessageModel());
        self.NewPrivateMessage().UserId(window._user.Id);
        self.NewPrivateMessage().UserName = window._user.UserName;
        self.NewPrivateMessage().ProfileFileStoreId(window._user.ProfileFileStoreId);
        if (self.CurrentPrivateMessageHeader().Id != null) {
            self.NewPrivateMessage().PrivateMessageHeaderId = self.CurrentPrivateMessageHeader().Id;
        }
    }

    function initialiseCurrentMessageHeader(header) {

        self.CurrentPrivateMessageHeader(header);
        initialiseNewPrivateMessage();
        self.Messages.removeAll();

        // if this is a new header, set it up
        if (IsNull(self.CurrentPrivateMessageHeader().Id)) {

            // add the current user as a header user
            var headerUser = new window.PrivateMessageHeaderUserModel();
            headerUser.User = window._user;
            self.CurrentPrivateMessageHeader().Users.push(headerUser);
            return;
        }

        // otherwise if it's an existing header then load the messages
        self.PopulateMessages(20, 0);
    }

    function resetUrl() {
        window.ChangeUrl("/intercom");
    }

    // adds a user to the current private message header
    // returns true if complete, returns false if not (i.e. would be a dupe user)
    function addUserToMessageHeader(user) {
        Log("addUserToMessageHeader()");

        // don't add duplicates
        if (!IsNull(_.find(self.CurrentPrivateMessageHeader().Users(), function (hu) { return hu.User.Id === user.Id; }))) {
            Log("User already added to header");
            return false;
        }

        // only allow so many recipients
        if (self.CurrentPrivateMessageHeader().Users().length === parseInt(window._maxHeaderUserCount)) {
            window.ShowEventMessage("error", "Maximum recipient count reached, sorry");
            return false;
        }

        var headerUser = new window.PrivateMessageHeaderUserModel();
        headerUser.User = user;

        self.CurrentPrivateMessageHeader().Users.push(headerUser);

        // tell the server about additions to existing headers
        if (!IsNull(self.CurrentPrivateMessageHeader().Id)) {
            // create a system message for this action and add it to the UI
            var systemMessage = new PrivateMessageModel();
            systemMessage.Type = "UserAdded";
            systemMessage.UserName = user.UserName;
            self.Messages.unshift(systemMessage);

            $.post("/api/intercom/AddMesageHeaderUser?headerId=" + self.CurrentPrivateMessageHeader().Id + "&userId=" + user.Id).
            fail(function () {
                window.ShowEventMessage("error", _notificationGeneralErrorText);
            });
        }

        return true;
    }

    // causes the focus (plain text or rich text) editor control to receive cursor focus
    function focusOnEditor() {
        if (self.DesktopView()) {
            $("#editor-desktop").redactor("focus.start");
        } else {
            $("#editor-mobile").focus();
        }
    }

    function startNewMessageToUser() {

        Log("newMessageDirectToUser()");
        var userId = window.QueryString.u;

        // do we have a header already with just this user in?
        // if so, re-use that header, otherwise start a new one

        $.ajax({
            type: "GET",
            url: "/api/intercom/GetHeaderToSingleUser?userId=" + userId,
            context: this,
            dataType: "json",
            success: function (jsonHeader) {

                // we have a header already
                // is it in our header list? 
                //      if so select it
                //      if not, add it to the header list at the top

                var header = new window.PrivateMessageHeaderModel(jsonHeader);
                if (IsNull(_.find(self.Headers(), function(h) { return h.Id === header.Id; }))) {
                    // header not in list, add the one we got from the server to the top of the list
                    Log("header found but not in list, adding");
                    self.Headers.unshift(header);
                }

                Log("header found, showing");
                self.ShowHeader(header.Id);

                focusOnEditor();
                resetUrl();
                self.ProcessingMessageUserDirect(false);

            }
        }).fail(function (response) {

            if (response.status === 404) {

                // no such header found, create a new one
                $.ajax({
                    type: "GET",
                    url: "/api/users/GetUser?userId=" + userId,
                    context: this,
                    dataType: "json",
                    success: function (jsonUser) {

                        Log("header not found, starting a new one");
                        self.StartNewMessage(false);
                        
                        Log("startNewMessageToUser(): adding user to header");
                        var user = new window.UserLightModel(jsonUser);
                        addUserToMessageHeader(user);
                        focusOnEditor();
                        resetUrl();

                    }
                }).fail(function (getUserFailResponse) {
                    Log(getUserFailResponse);
                    window.ShowEventMessage("error", _notificationGeneralErrorText);
                    resetUrl();
                });

            } else {
                Log(response);
                window.ShowEventMessage("error", _notificationGeneralErrorText);
                resetUrl();
            }
        });

    }

    function highlightHeader(headerId) {
        var headerElement = $("#header-" + headerId + " .intercom-header-inner");
        headerElement.addClass("intercom-header-inner-highlighted").delay(2000).queue(function () {
            headerElement.removeClass("intercom-header-inner-highlighted");
        });
    }

    function markMessageAsRead(messageId) {
        $.post("/api/intercom/MarkMessageAsRead?messageId=" + messageId, null, function() {
            Log("marked message as read: " + messageId);
            addMessageReadBy(messageId);
        }).fail(function() {
            Log("Couldn't mark a message as read! " + messageId);
        });
    }

    function updateHeader(header) {

        //Log("updateHeader()");
        var headerModel = new window.PrivateMessageHeaderModel(header);
        var headersBox = $("#intercom-headers-box");

        // is header already in the list?
        var headerIsInList = false;
        for (var i = 0; i < self.Headers().length; i++) {
            if (self.Headers()[i].Id === headerModel.Id) {

                // the header is an existing one already in the list.
                // has the LastMessageCreated date changed, i.e. more recent than the 
                // first header in the lists, to signify it has new, unread messages?

                // remove current header, add new header to bottom, then sort list
                Log("replacing header and sorting");
                self.Headers.splice(i, 1);
                self.Headers.push(headerModel);
                self.Headers.sort(function (left, right) {
                    return SortPrivateMessageHeaders(left, right);
                });

                if (headerModel.LastMessageCreated() > self.Headers()[i].LastMessageCreated()) {
                    // existing header, new message
                    headersBox.animate({ scrollTop: 0 }, "fast");
                    if (self.CurrentPrivateMessageHeader().Id !== headerModel.Id) {
                        Log("highlighting as has new message");
                        highlightHeader(headerModel.Id);
                    }
                }

                headerIsInList = true;
                break;
            }
        }

        if (!headerIsInList) {
            Log("Adding new header to list and highlighting...");
            // add the header to the headers list and scroll the list to the top
            self.Headers.unshift(headerModel);
            headersBox.animate({ scrollTop: 0 }, "fast");
            highlightHeader(headerModel.Id);
        }

    }

    function receiveMessage(jsonMessage) {

        // if the message header is open, add it to the messages list and scroll to the top of the messages div and mark it as read
        if (jsonMessage.PrivateMessageHeaderId === self.CurrentPrivateMessageHeader().Id) {

            var message = new window.PrivateMessageModel(jsonMessage);
            self.Messages.unshift(message);
            $("#intercom-messages-box").animate({ scrollTop: 0 }, "fast");

            // mark it as read, as the header is open and we've scrolling into view
            markMessageAsRead(message.Id);

        }
    }

    $(window).resize(function () {
        setTimeout(function () {
            self.ResizeLayout();
        }, 100);
    });

    // --[ HEADERS ]------------------------------------------------------------

    // tell infinite-scroll about the current scroll position
    // (debounce is important for ie11 smooth scroll problems! and also generally nicer in all browsers)
    $("#intercom-headers-box").scroll(_.debounce(function () {
        self.Headers.infinitescroll.scrollY($("#intercom-headers-box").scrollTop());

        // add more items if scroll reaches the last x items
        var lastVisibleIndex = self.Headers.infinitescroll.lastVisibleIndex.peek();
        var headersLength = self.Headers.peek().length;
        var remaining = headersLength - lastVisibleIndex;

        if (remaining > 0 && remaining <= 100) {
            self.PopulateHeaders(20, lastVisibleIndex);
        }
    }, 250));

    // update dimensions of infinite-scroll viewport and item
    self.UpdateHeadersScrollerDimensions = function () {
        var headersBox = $("#intercom-headers-box");
        self.Headers.infinitescroll.viewportWidth(headersBox.innerWidth());
        self.Headers.infinitescroll.viewportHeight(headersBox.innerHeight());
        self.Headers.infinitescroll.itemWidth(headersBox.innerWidth());
        // would like to get this from the css class ideally!
        self.Headers.infinitescroll.itemHeight(90);
    }

    self.PopulateHeaders = function (itemsToRetrieve, lastVisibleIndex) {

        // show a loading graphic if the get headers request takes longer than 100 ms
        var needToShowHeaderLoader = true;
        setTimeout(function() {
            if (needToShowHeaderLoader) {
                self.ShowingHeaderLoader(true);
            }
        }, 250);

        self.HeadersBeingRetrieved(true);

        $.ajax({
            type: "GET",
            url: "/api/intercom/GetHeaders?limit=" + itemsToRetrieve + "&startIndex=" + lastVisibleIndex + "&filterUnread=" + self.HeadersFilterUnread(),
            context: this,
            dataType: "json",
            success: function (jsonHeaders) {

                for (var i = 0; i < jsonHeaders.length; i++) {

                    // convert the json entities to models
                    // don't add headers that already exist in the list.
                    // this could happen if the lastVisibleIndex value is not accurate and we just keep asking for new headers.
                    var isUnique = true;
                    for (var x = 0; x < self.Headers().length; x++) {

                        if (self.Headers()[x].Id === jsonHeaders[i].Id) {
                            isUnique = false;
                            break;
                        }
                    }

                    if (isUnique) {
                        self.Headers.push(new window.PrivateMessageHeaderModel(jsonHeaders[i]));
                    }
                }

                // headers added
                // select a header if we have a query-string instruction for it
                if (!self.ActionedShowHeaderByUri() && !IsNullOrEmpty(window.QueryString.h)) {
                    var headerId = parseInt(window.QueryString.h);
                    self.ShowHeader(headerId);
                    self.ActionedShowHeaderByUri(true);
                }

                // make sure we either don't show the header as the results have come back immediately
                // or remove the header loader if we have shown it because the request has taken some time
                needToShowHeaderLoader = false;
                self.ShowingHeaderLoader(false);
                self.HeadersBeingRetrieved(false);

                self.ResizeLayout();
            }
        }).fail(function() {
            window.ShowEventMessage("error", _notificationGeneralErrorText);
        });
    }

    self.RemoveSelfFromHeader = function () {
        $("#remove-self-modal").modal();
    }

    self.Leave = function () {
        $("#leave-modal").modal();
    }

    self.ExecuteLeave = function () {
        $("#leave-modal").modal("hide");
        executeRemoveSelfFromHeader();
    }

    self.Delete = function () {
        $("#delete-modal").modal();
    }

    self.ExecuteDelete = function () {
        $("#delete-modal").modal("hide");
        executeRemoveSelfFromHeader();
    }
    
    // causes a loaded header to be displayed.
    // called from both the view and view-model.
    self.ShowHeader = function (showHeaderId) {

        // make sure the header isn't already being shown
        if (!IsNull(self.CurrentPrivateMessageHeader()) && self.CurrentPrivateMessageHeader().Id === showHeaderId) {
            //Log("ShowHeader(): header already being shown");
            return;
        }

        var header = _.find(self.Headers(), function (h) { return h.Id === showHeaderId });
        if (IsNull(header)) {
            //Log("ShowHeader(): header id not found: " + showHeaderId);
            resetUrl();
            return;
        }

        // cause the header to be selected and the messages to be shown
        initialiseCurrentMessageHeader(header);
        self.ShowingHeader(true);
        self.ResizeLayout();
    };

    self.ShowAllHeaders = function () {
        Log("Showing all headers");
        self.HeadersFilterUnread(false);
        self.Headers.removeAll();
        self.PopulateHeaders(20, 0);

        // save the preference to a cookie (or update an existing one)
        $.cookie("ihf", "all", { expires: 9999 });
    };

    self.ShowUnreadHeaders = function() {
        Log("Showing only headers with unread messages in");
        self.HeadersFilterUnread(true);
        self.Headers.removeAll();
        self.PopulateHeaders(20, 0);
        self.ResizeLayout();

        // save the preference to a cookie (or update an existing one)
        $.cookie("ihf", "unread", { expires: 9999 });
    };

    self.MarkEveryMessageAsRead = function () {

        var $popup = null;
        var needToShowLoader = true;

        // show a loader if necessary - this op can take a while
        // don't show the popup immediately, some pics might upload so quick it'll cause a flicker.
        setTimeout(function () {

            // if the upload completes before the timer finishes, don't show the popup
            if (!needToShowLoader) {
                return;
            }

            $popup = new $.Popup({
                content: function () {
                    return String.format(window._loadingPopupTemplate, "One moment...");
                }
            });
            $popup.open();

        }, 200);

        Log("Marking every message as read...");
        $.post("/api/intercom/MarkEveryMessageAsRead", null, function () {

            needToShowLoader = false;
            if (!IsNull($popup)) {
                $popup.close();
            }

            // ensure all message headers are marked as having no unread messages in our client models
            $.each(self.Headers(), function (index, header) {
                header.HasUnreadMessagesForCurrentUser(false);
            });

            if (self.HeadersFilterUnread()) {
                self.ShowAllHeaders();
            }

            window.ShowEventMessage("success", "Every message marked as read");

        }).fail(function () {
            window.ShowEventMessage("error", _notificationGeneralErrorText);
        });

    };

    // --[ MESSAGE CRUD ]-------------------------------------------------------
    
    // tell infinite-scroll about the current scroll position
    // (debounce is important for ie11 smooth scroll problems! and also generally nicer in all browsers)
    $("#intercom-messages-box").scroll(_.debounce(function () {

        // this can sometimes fire when we're not viewing any message. I think this is to do with resizing.
        if (!self.ShowingHeader()) {
            return;
        }

        self.Messages.infinitescroll.scrollY($("#intercom-messages-box").scrollTop());

        // add more items if scroll reaches the last x items
        var lastVisibleIndex = self.Messages.infinitescroll.lastVisibleIndex.peek();
        var messagesLength = self.Messages.peek().length;
        var remaining = messagesLength - lastVisibleIndex;

        if (remaining > 0 && remaining <= 20) {
            self.PopulateMessages(20, lastVisibleIndex);
        }

    }, 250));

    // this needs to be done up-front so view bindings work from the go
    initialiseCurrentMessageHeader(new window.PrivateMessageHeaderModel());
    
    // allows the user to create a new message. this can be called from the view or the view-model
    self.StartNewMessage = function (showUserBox) {

        if (IsNull(showUserBox)) {
            showUserBox = true;
        }

        self.ShowingHeader(true);
        self.ShowingUserBox(showUserBox);
        $("#i1").focus();

        // if we have a persisted message open, create a header otherwise use the current new one
        if (!IsNull(self.CurrentPrivateMessageHeader().Id)) {
            initialiseCurrentMessageHeader(new window.PrivateMessageHeaderModel());
        }

        self.ResizeLayout();
	};

    self.CloseMessage = function () {

	    self.ShowingHeader(false);
	    self.ShowingUserBox(false);
	    self.UserSearchTerm("");

	    // this would be null if it's a newly-created header
	    if (self.SelectedHeaderElement != null) {
	        self.SelectedHeaderElement.removeClass("intercom-header-selected");
	    }

	    initialiseCurrentMessageHeader(new window.PrivateMessageHeaderModel());
	    self.ResizeLayout();
	};

    self.ProcessUserSearchResultClick = function (user) {

	    if (!addUserToMessageHeader(user)) {
	        Log("ProcessUserSearchResultClick(): user already in header, not continuing");
	        return;
	    }

        // UI changes necessary
	    self.UserSearchResults.removeAll();
	    self.UserSearchTerm("");
	    self.ResizeUnderUserSearchBox();
	};

	self.RemoveHeaderUser = function (headerUser) {
	    self.UserBeingRemoved(headerUser);
	    $("#remove-user-modal").modal();	    
	};

	self.ExecuteRemoveHeaderUser = function () {

	    self.CurrentPrivateMessageHeader().Users.remove(self.UserBeingRemoved());
	    self.ResizeUnderUserSearchBox();

	    // tell the server about removals from existing headers
	    if (self.CurrentPrivateMessageHeader().Id != null) {

	        // create a system message for this action and add it to the UI
	        var systemMessage = new PrivateMessageModel();
	        systemMessage.Type = "UserRemoved";
	        systemMessage.UserName = self.UserBeingRemoved().User.UserName;
	        self.Messages.unshift(systemMessage);

	        $.post("/api/intercom/RemoveMesageHeaderUser?headerId=" + self.CurrentPrivateMessageHeader().Id + "&userId=" + self.UserBeingRemoved().User.Id, function () {
	            $("#remove-user-modal").modal("hide");
	            self.UserBeingRemoved(null);
	        }).fail(function () {
	            $("#remove-user-modal").modal("hide");
	            window.ShowEventMessage("error", _notificationGeneralErrorText);
	            self.UserBeingRemoved(null);
	        });

	    } else {
	        $("#remove-user-modal").modal("hide");
	    }
    }
	
	self.UserSearchTerm.subscribe(function (term) {

	    if (window.IsNullOrEmpty(term)) {
	        self.UserSearchResults.removeAll();
	        self.ShowingUserSearchNoResults(false);
	        self.ResizeLayout();
	        return;
	    }

	    // show a loader graphic if the request takes 200ms or longer, i.e. after an app-restart
	    var needToShowLoader = true;
	    setTimeout(function () {
	        if (needToShowLoader) {
	            self.ShowingUserSearchLoader(true);
	            self.ResizeLayout();
	        }
	    }, 200);

	    // could do with some rate-throttling/protection to safeguard server response times...	    
	    $.ajax("/api/users/SearchUsers?maxResults=10&term=" + term).done(function (results) {

	        // convert the json entities to models
	        self.UserSearchResults.removeAll();
	        for (var i = 0; i < results.length; i++) {
	            self.UserSearchResults.push(new window.UserLightModel(results[i]));
	        }

	        self.ShowingUserSearchLoader(false);
	        self.ShowingUserSearchNoResults(false);
	        needToShowLoader = false;
	        self.ResizeLayout();

	    }).fail(function (response) {

	        if (response.status === 404) {
	            self.UserSearchResults.removeAll();
	            self.ShowingUserSearchNoResults(true);
	            self.ShowingUserSearchLoader(false);
	            needToShowLoader = false;
	        } else {
	            self.UserSearchResults.removeAll();
	            self.ShowingUserSearchNoResults(false);
	            self.ShowingUserSearchLoader(false);
	            needToShowLoader = false;
	            window.ShowEventMessage("error", _notificationGeneralErrorText);
	        }
	        self.ResizeLayout();

	    });

	});

	self.SendMessage = function () {

	    self.ShowingUserBox(false);
	    self.ResizeUnderUserSearchBox();

	    // to reduce the chance of duplicate messages, disable the submit button until the request is complete.
	    self.ProcessingSendMessage(true);

	    // is this a new message session? do we need to create the header first?
	    if (self.CurrentPrivateMessageHeader().Id === null)
	    {
	        var jsonCurrentPrivateMessageHeader = ko.toJSON(self.CurrentPrivateMessageHeader());
	        $.ajax({
	            url: "/api/intercom/CreateMessageHeader",
	            type: "POST",
	            data: jsonCurrentPrivateMessageHeader,
	            contentType: "application/json;charset=utf-8",
	            success: function (newHeaderId) {

	                self.CurrentPrivateMessageHeader().Id = newHeaderId;

	                // add the new header to the headers list
	                self.Headers.unshift(self.CurrentPrivateMessageHeader());

                    // now create the message itself
	                self.NewPrivateMessage().PrivateMessageHeaderId = self.CurrentPrivateMessageHeader().Id;
	                self.ExecuteMessageSend();
	            }
	        }).fail(function () {
	            window.ShowEventMessage("error", _notificationGeneralErrorText);
	            self.ProcessingSendMessage(false);
	            return;
	        });
	    }
	    else
	    {
	        self.ExecuteMessageSend();
	    }
	};

	self.ExecuteMessageSend = function () {

	    // add the message to the UI
	    self.Messages.unshift(self.NewPrivateMessage());

	    // scroll to the top of the messages list so it can be seen
	    $("#intercom-messages-box").animate({ scrollTop: 0 }, "fast");

	    var jsonNewPrivateMessage = ko.toJSON(self.NewPrivateMessage());
	    $.ajax({
	        url: "/api/intercom/CreateMessage",
	        type: "POST",
	        data: jsonNewPrivateMessage,
	        contentType: "application/json;charset=utf-8",
	        success: function () {

	            // start a new message object
	            initialiseNewPrivateMessage();
	            self.ProcessingSendMessage(false);

	            // update the header message last sent date (server will send this on next request anyhow)
	            var newMessageHeader = _.find(self.Headers(), function (h) { return h.Id === self.CurrentPrivateMessageHeader().Id; });
	            newMessageHeader.LastMessageCreated(new Date());

	            // make sure the header is at the top of the header list (sort the headers)
	            self.Headers.sort(function(left, right) {
	                return SortPrivateMessageHeaders(left, right);
	            });
	        }
	    }).fail(function () {
	        window.ShowEventMessage("error", _notificationGeneralErrorText);
	        self.ProcessingSendMessage(false);

	        // remove the pending message from the ui...
	        self.Messages.shift();
	    });

	};

    self.CanSendMessage = ko.pureComputed(function () {
	    return self.NewPrivateMessage().IsValid() === true && self.CurrentPrivateMessageHeader().IsValid() === true && self.ProcessingSendMessage() === false;
	}, self);

    // update dimensions of infinite-scroll viewport and item
	self.UpdateMessagesScrollerDimensions = function () {
	    var messagesBox = $("#intercom-messages-box");
	    self.Messages.infinitescroll.viewportWidth(messagesBox.innerWidth());
	    self.Messages.infinitescroll.viewportHeight(messagesBox.innerHeight());
	    self.Messages.infinitescroll.itemWidth(messagesBox.innerWidth());
	    // err, this is totally variable depending on the length of the message content!?
	    self.Messages.infinitescroll.itemHeight(50);
	}

	self.PopulateMessages = function (itemsToRetrieve, lastVisibleIndex) {

	    $.ajax({
	        type: "GET",
	        url: "/api/intercom/GetMessages?headerId=" + self.CurrentPrivateMessageHeader().Id + "&limit=" + itemsToRetrieve + "&startIndex=" + lastVisibleIndex + "&markAsRead=true",
	        context: this,
	        dataType: "json",
	        success: function (jsonMessages) {

                // todo: could this be simplified using jQuery equivalents of underscore uniqueness functions?

	            // convert the json entities to models
	            for (var i = 0; i < jsonMessages.length; i++) {
	                // don't add messages that already exist in the list.
	                // this could happen if the lastVisibleIndex value is not accurate and we just keep asking for new messages.
	                var isUnique = true;
	                for (var x = 0; x < self.Messages().length; x++) {
	                    if (self.Messages()[x].Id === jsonMessages[i].Id) {
	                        isUnique = false;
	                        break;
	                    }
	                }

	                if (isUnique) {
	                    self.Messages.push(new window.PrivateMessageModel(jsonMessages[i]));
	                }
	            }

	            // messages added
	            // select a message if we have a Uri instruction for it
	            if (!self.ActionedShowMessageByUri() && !IsNullOrEmpty(window.QueryString.m)) {
	                var messageId = parseInt(window.QueryString.m);
	                resetUrl();

                    // highlight the message content
	                var msgElement = $("#msg-" + messageId + " .intercom-message-content");
	                msgElement.addClass("intercom-message-content-highlighted").delay(100).queue(function () {
	                    msgElement.removeClass("intercom-message-content-highlighted");
	                });
	                self.ActionedShowMessageByUri(true);
	            }

	        }
	    }).fail(function () {
	        window.ShowEventMessage("error", _notificationGeneralErrorText);
	    });
	}

	self.ShowUserBox = function () {
	    self.ShowingUserBox(true);
	    self.ResizeRightColInnerTop();
	}

	self.HideUserBox = function () {
	    self.ShowingUserBox(false);
	    self.UserSearchTerm("");
	    self.UserSearchResults.removeAll();
	    self.ShowingUserSearchNoResults(false);
	    self.ShowingUserSearchLoader(false);
	    self.ResizeRightColInnerTop();
	}

	self.MarkAllMessagesInHeaderAsRead = function () {

        // tell the sever to mark all messages as read
	    $.post("/api/intercom/MarkAllMessagesInHeaderAsRead?headerid=" + self.CurrentPrivateMessageHeader().Id, null, function () {

	        // ensure all messages are read in our client models
	        $.each(self.Messages(), function (index, message) {

                // each message needs a read-by record
	            addMessageReadBy(message.Id);

	        });

            // mark the client header as having no unread messages
	        self.CurrentPrivateMessageHeader().HasUnreadMessagesForCurrentUser(false);
	        window.ShowEventMessage("success", "All messages marked as read");

	    }).fail(function() {
	        window.ShowEventMessage("error", _notificationGeneralErrorText);
	    });
	};

    // --[ RESIZING ]-----------------------------------------------------------	

	self.DesktopView = ko.observable(false);

    self.ShowHeaders = ko.pureComputed(function () {
	    return self.DesktopView() || !self.DesktopView() && !self.ShowingHeader();
	}, self);

    self.ShowMessages = ko.pureComputed(function () {
	    return self.DesktopView() || !self.DesktopView() && self.ShowingHeader();
	}, self);

	self.ResizeHeaders = function () {
	    var headersBoxParent = $("#intercom-left-col");
	    var headersBox = $("#intercom-headers-box");

	    // need to squash the headers first so the parent can shrink back too
	    // otherwise on window reductions the whole column won't shink down, or for long lists will grow too big!
	    headersBox.height(0);
	    var headersBoxNewHeight = headersBoxParent.innerHeight() - headersBox.position().top;
	    headersBox.height(headersBoxNewHeight);
	    self.UpdateHeadersScrollerDimensions();
	}

	self.ResizeRightColInner = function (outerBoxHeight) {
	    // make the inner fill the parent element. can't use CSS3 flex for this as the parent is using display:table-cell
	    var rightColInner = $("#intercom-right-col-inner");
	    rightColInner.height(outerBoxHeight);
	    self.ResizeRightColInnerTop();
	}

	self.ResizeRightColInnerTop = function () {
	    var rightColInner = $("#intercom-right-col-inner");
	    var rightColInnerTop = $("#intercom-right-col-inner-top");
	    var rightColInnerBottom = $("#intercom-right-col-inner-bottom");
	    var rightColInnerTopNewHeight = rightColInner.innerHeight() - rightColInnerBottom.outerHeight();
	    rightColInnerTop.height(rightColInnerTopNewHeight);
	    self.ResizeUnderUserSearchBox();
	}

	self.ResizeUnderUserSearchBox = function () {
	    // now ensure the user search results box cannot break out underneath the intercom-right-col-inner-bottom
	    var rightColInnerTop = $("#intercom-right-col-inner-top");
	    var menuStip = $("#intercom-right-col-menu-strip");
	    var userSearchResults = $("#intercom-user-search-results");
	    var userSearchBox = $("#intercom-user-search-box");
	    var userSearchResultsNewHeight = rightColInnerTop.height() - userSearchBox.outerHeight() - menuStip.outerHeight();
	    userSearchResults.height(userSearchResultsNewHeight);
	    self.ResizeMessagesBox();
	}

	self.ResizeMessagesBox = function () {
	    var messagesBoxParent = $("#intercom-right-col-inner-top");
	    var messagesBox = $("#intercom-messages-box");
	    var messagesBoxPaddingTop = parseInt(messagesBox.css("padding-top"));
	    var messagesBoxPaddingBottom = parseInt(messagesBox.css("padding-bottom"));
	    var messagesBoxNewHeight = messagesBoxParent.innerHeight() - messagesBox.position().top - messagesBoxPaddingTop - messagesBoxPaddingBottom;
	    messagesBox.height(messagesBoxNewHeight);
	    self.UpdateMessagesScrollerDimensions();
	}

    self.GetFirstNonCurrentUserProfilePicture = function(firstNonCurrentUser) {
        if (firstNonCurrentUser != null) {
            return window.GetUserProfileGraphic("medium", firstNonCurrentUser.UserName, firstNonCurrentUser.ProfileFileStoreId);
        } else {
            // this is weird
            Log("GetFirstNonCurrentUserProfilePicture: No FirstNonCurrentUser() found");
            return "";
        }
    }

    self.GetFirstNonCurrentUserName = function (firstNonCurrentUser) {
        if (!IsNull(firstNonCurrentUser)) {
            return firstNonCurrentUser.UserName;
        } else {
            return "";
        }
    }

	self.ResizeLayout = function () {

	    // mobile or desktop? if mobile don't show the right column
        // desktop from ipad (768 wide) and up

	    var width = $(window).width();
	    if (width < 768 && self.DesktopView()) {
	        self.DesktopView(false);
	    }

	    if (width >= 768 && !self.DesktopView()) {
	        self.DesktopView(true);
	    }

	    var outerBox = $("#intercom-outer-box");
	    outerBox.show();

	    var footer = $("footer");
	    if (!self.DesktopView()) {
	        footer.remove();
	    }

	    var outerBoxHeight = $(window).height() - outerBox.offset().top - footer.outerHeight();
	    if (self.DesktopView()) {
	        outerBoxHeight = outerBoxHeight - 10;
	    }

	    outerBox.height(outerBoxHeight);
	    self.ResizeHeaders();
	    self.ResizeRightColInner(outerBoxHeight);
	}

    // --[ PRIVATE FUNCTIONS ]-------------------------------------------------

	function init() {

	    // does the user have a preference for header filtering?
	    var headerFilterPreference = $.cookie("ihf");
	    switch (headerFilterPreference) {
	        case "unread":
	            self.HeadersFilterUnread(true);
	            break;
	    }

        self.ResizeLayout();

        if (IsNull(window.QueryString.nm) || window.QueryString.nm !== "1") {

            //Log("no uri instruction");
            self.ResizeLayout();
            self.PopulateHeaders(20, 0);

        } else {

            if (self.DesktopView() === true) {

                //Log("uri instruction on desktop");
                self.ProcessingMessageUserDirect(true);
                self.ResizeLayout();
                self.PopulateHeaders(20, 0);
                startNewMessageToUser();

            } else {

                //Log("uri instruction on mobile");
                self.ProcessingMessageUserDirect(true);
                startNewMessageToUser();

                // don't want these to show in initial load as a flicker!
                // but they need to be there when you return to the headers
                self.PopulateHeaders(20, 0);

            }

        }

        self.IntercomHub = $.connection.intercomHub;
        self.IntercomHub.client.updateHeader = updateHeader;
	    self.IntercomHub.client.receiveMessage = receiveMessage;
        Log("Intercom: all hub proxy event bindings made");

	    window.StartHub();
	}

    // --[ INIT ]---------------------------------------------------------------

    init();
};