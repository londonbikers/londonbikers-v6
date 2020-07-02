// used to determine whether the browser supports the HTML5 history feature.
// which in turn is used for slick non-page-loading navigation and to allow a fall-back for older browsers.
var _isHistorySupported = true;
var _lastUsedProfilePhotoInitialVariant = 0;
var _lastUsedProfilePhotoInitialvariantUserName = "";
var _defaultPageSize = 25;
var _postHubStartTasks = [];
//$.connection.hub.logging = true;

$(function () {

    if (!window.Modernizr.history) {
        _isHistorySupported = false;
    }

    CheckForEventMessage();
    if (!IsMobile()) {
        $("[data-toggle=\"tooltip\"]").tooltip();
        $("[data-tooltip=\"true\"]").tooltip();
    }

    var $header = $("header");
    if ($header.length > 0) {
        ko.applyBindings(new HeaderViewModel(), $("header")[0]);
    }

    var $postControlModal = $("#post-control-modal");
    if ($postControlModal.length > 0) {
        ko.applyBindings(new PostControlViewModel(), $postControlModal[0]);
    }
    
    $("#anonModal").on("hide.bs.modal", function() {
        $("#anon-header").show();
        Log("shown anon-header");
    });

    $(".selectpicker").selectpicker({
        style: "btn-default",
        size: "auto"
    });

});

// ----------------------------------------------------------------------------------------------------------

function IsUserLoggedIn() {
    return !IsNull(window._user);
}

// used for checking if a string is null/undefined or empty
function IsNullOrEmpty(text) {
    return IsNull(text) || text === "";
}

// used for checking if any object is null or undefined
function IsNull(object) {
    return (object === null || typeof object === "undefined");
}

// causes an event message to be shown to the user on the next page transition
function SetEventMessage(type, message) {
    var kvp = type + ":" + message;
    $.cookie("EventMessage", kvp, { expires: 7, path: "/" });
}

function CheckForEventMessage() {
    var kvp = $.cookie("EventMessage");
    if (kvp != undefined) {
        var message = kvp.split(":");
        ShowEventMessage(message[0], message[1]);
        $.removeCookie("EventMessage", { path: "/" });
    }
}

function ShowEventMessage(type, message) {
    // ensure the notification is not in the DOM already
    $("#notification").remove();

    // build the template
    var notification = String.format(window._notificationTemplate, message);

    // get the right CSS class for the notification type
    var cssClass = "";
    if (type === "info") {
        cssClass = "notification-info";
    } else if (type === "error") {
        cssClass = "notification-error";
    } else if (type === "success") {
        cssClass = "notification-success";
    }

    // add to the DOM
    $("body").prepend(notification);
    $("#notification").miniNotification({
        hideOnClick: true,
        time: 3000,
        opacity: 1,
        innerDivClass: cssClass,
        onHide: function () {
            // we need to remove it after as it leaves a drop-shadow effect in place otherwise.
            // with a delay to happen after the hiding animation finishes.
            setTimeout(function () {
                $("#notification").remove();
            }, 500);
        }
    });
}

String.format = function () {
    var s = arguments[0];
    for (var i = 0; i < arguments.length - 1; i++) {
        var reg = new RegExp("\\{" + i + "\\}", "gm");
        s = s.replace(reg, arguments[i + 1]);
    }
    return s;
};

function EncodeUrlPart(component) {
    if (component == undefined) {
        return "";
    }

    component = component.replace(/\s/g, "-"); // change space to hyphen
    component = component.replace(/[^a-zA-Z\d-]/gi, ""); // only allow letters, numbers and hyphens
    component = component.replace(/-{2,}/g, "-"); // remove additional hyphens
    component = component.replace(/^[^a-z\d]*|[^a-z\d]*$/gi, ""); // trim any non alpha-numerics from the start and end
    component = component.toLowerCase();
    return component;
}

function NumberWithCommas(number) {
    if (IsNullOrEmpty(number)) {
        return "0";
    }
    return number.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ",");
}

// formats content for stand-alone use, i.e. not necessarily for a post
function FormatContent(content) {

    if (!IsNullOrEmpty(content)) {

        // replace nbsp's as these interfere with other matches
        content = content.replace(/&nbsp;/gmi, " ");

        // convert loose URLs to links
        content = content.replace(/(\s|^|>)(https?\:\/\/.*?)(\s|$|<)/gmi, '$1<a href="$2">$2</a>$3');

        // when v6 launched it didn't use rich-text editors so content was persisted with just new lines
        // later on rich-text editors were added which inserted html. this function needs to accommodate both content formats
        // by converting new liens still but not ending up doubling up new lines for rich-text created posts. we do this
        // by determining if there's any <p> tags in, which indicate a rich-text post. in this case don't convert new lines.

        if (content.indexOf("<p>") === -1) {
            content = content.replace(/\r\n/gmi, "<br />"); // legacy content conversion
            content = content.replace(/\n/gmi, "<br />"); // v6 content conversion
        }

        // forum code conversions...
        content = content.replace(/\[youtube\].*?v=(.*?)(?:&.*?)?\[\/youtube\]/gmi, '<iframe class="embed-responsive-item" src="https://www.youtube.com/embed/$1"></iframe>');
        content = content.replace(/\[quote\](.*?)\[hr\](.*?)\[\/quote\]/gmi, "<blockquote><p>$1</p><footer>$2</footer></blockquote>");
        content = content.replace(/(?:\[quote\])(.*?)(?:\[\/quote\])/gmi, "<blockquote><p>$1</p></blockquote>");
        content = content.replace(/\[url\](.*?)\[\/url\]/gi, '<a href="$1">$1</a>');
        content = content.replace(/\[url=(.*?)](.*?)\[\/url\]/gi, '<a href="$1">$2</a>');
        // M: Do not make images responsive yet as this will happen later, doing so here causes unwanted nesting
        content = content.replace(/\[img\](.*?)\[\/img\]/gi, '<img src="$1" class="br3" />');
        // M: Some image sites produce code using [IMG=<href>] format
        content = content.replace(/\[img=(.*?)]/gi, '<img src="$1" class="br3" />');
        content = content.replace(/\[(.+?)\]/g, "<$1>");

        // M: Replaced \s*? with \s(?:[^>]*?\s)? after the tag and (?:.*?) at the end for all subsequent rules so attributes before an HREF do not cause them to fail.

        // convert links to YouTube videos into embedded videos
        // M: Same change as before to use a single rule for www.youtube.com links regardless of additional URL parameters.
        content = content.replace(/<a\s(?:[^>]*?\s)?href="https?:\/\/www\.youtube\.com\/watch\?(?:.*?)v=(.*?)(?:&.*?)?"(?:.*?)>(?:.*?)<\/a>/gmi, '<div class="embed-responsive embed-responsive-16by9 br3"><iframe class="embed-responsive-item" src="https://www.youtube.com/embed/$1" ></iframe></div>');
        content = content.replace(/<a\s(?:[^>]*?\s)?href="https?:\/\/youtu\.be\/(.*?)"(?:.*?)>(?:.*?)<\/a>/gmi, '<div class="embed-responsive embed-responsive-16by9 br3"><iframe class="embed-responsive-item" src="https://www.youtube.com/embed/$1" ></iframe></div>');
        content = content.replace(/<a\s(?:[^>]*?\s)?href="https?:\/\/m.youtube.com\/watch\?(?:.*?)v=(.*?)&.*?"(?:.*?)>(?:.*?)<\/a>/gmi, '<div class="embed-responsive embed-responsive-16by9 br3"><iframe class="embed-responsive-item" src="https://www.youtube.com/embed/$1"></iframe></div>');

        // convert links to images into images
        // M: Should the content of a link be discarded when it may have useful description of the image as a caption?
        // M: Version below instead also captures the content, wraps it in a div so can be formatted as desired, and removes an image 
        // M: in one contained to avoid double images. CAVEAT: only the last image is removed if more are included.
        content = content.replace(/<a\s(?:[^>]*?\s)?href=(['"])((?:(?!\1).)*(?:\.png|\.jpe?g|\.gif|\.webp|\.bmp))\1(?:.*?)>(.*?)(?:<img\s[^>]*?(?:\/>|><\/img>|>))*((?:(?!<img\s).)*?)<\/a>/gmi, '<img src="$2" class="br3" /><div class="photo-link-as-caption"><span class="glyphicon glyphicon-new-window" aria-hidden="true" title="linked from another website"></span> $3$4</div>');
        // M: remove the caption div if there is nothing to display, either it only contained an image or was blank.
        content = content.replace(/<div class="photo-link-as-caption">\s*<\/div>/gmi, "");

        // convert IMG elements to responsive images
        // M: Might as well be single quote friendly here too, now we also make previously converted images responsive
        content = content.replace(/<img\s(?:[^>]*?\s)?src=(['"])(.*?)\1(?:.*?)(?:\/>|><\/img>|>)/gmi, '<div class="img-responsive"><a href="$2"><img src="$2" class="br3" /></a></div>');

        // add target to all links
        // M: Used \s after the tag as it may have a tab rather than a space
        content = content.replace(/(<a\s)/gi, '$1target="_blank" ');

        // make iframe embeds responsive in layout - makes Google maps look great
        if (content.match(/<iframe/gmi)) {
            if (content.match(/https:\/\/www\.google\.com\/maps\/embed/gmi)) {
                // google map embeds are an exception and can be displayed full-width, but with a height restriction
                content = content.replace(/(<iframe (?:.*?)<\/iframe>)/gi, '<div class="embed-responsive embed-responsive-16by9 br3">$1</div>');
                content = content.replace(/(<iframe )/gmi, '$1class="embed-responsive-item br3" ');
            } else {
                content = content.replace(/(<iframe (?:.*?)<\/iframe>)/gi, '<div class="embed-responsive embed-responsive-16by9 br3">$1</div>');
                content = content.replace(/(<iframe )/gmi, '$1class="embed-responsive-item" ');
            }
        }

        return content;
    } else {
        return null;
    }

}

// formats content for a post
function FormatPost(post) {

    var content = FormatContent(post);

    // to ensure UI spacing consistency we need to wrap plain-text posts in a P tag so they are spaced the same as rich-text posts
    if (!IsNullOrEmpty(content) && content.indexOf("<p>") !== 0) {
        content = "<p>" + content + "</p>";
    }

    return content;

}

// converts new-lines into html BR's. doesn't allow other html to rendered as-is
// doesn't do any other conversions, just new-lines, so no rich-post conversions. Use FormatContent() for that.
function SafeFormatText(text) {
    text = HtmlToText(text);
    text = text.replace(/\r\n/gmi, "<br />"); // legacy content conversion
    text = text.replace(/\n/gmi, "<br />"); // v6 content conversion
    return text;
}

var QueryString = function () {
    // This function is anonymous, is executed immediately and 
    // the return value is assigned to query-string!
    var queryString = {};
    var query = window.location.search.substring(1);
    var vars = query.split("&");
    for (var i = 0; i < vars.length; i++) {
        var pair = vars[i].split("=");
        // If first entry with this name
        if (typeof queryString[pair[0]] === "undefined") {
            queryString[pair[0]] = pair[1];
            // If second entry with this name
        } else if (typeof queryString[pair[0]] === "string") {
            var arr = [queryString[pair[0]], pair[1]];
            queryString[pair[0]] = arr;
            // If third or later entry with this name
        } else {
            queryString[pair[0]].push(pair[1]);
        }
    }
    return queryString;
}();

// removes an query-string parameter from a URL and updates the browser address to that new address
function RemoveParameterFromUrl(parameterToRemove) {

    if (window.location.href.indexOf("?") === -1) {
        Log("no qs");
        return;
    }

    var url = window.location.pathname;
    var newQueryString = _.omit(window.QueryString, parameterToRemove);

    var keys = _.keys(newQueryString);
    if (keys.length > 0) {
        url += "?";
        for (var i = 0; i < keys.length; i++) {
            url += keys[i] + "=" + newQueryString[keys[i]];
            if (i < (keys.length - 1)) {
                url += "&";
            }
        }
    }

    window.UpdateUrl(url);
}

function ChangeUrl(url) {
    Log("ChangeUrl()");
    if (!window._isHistorySupported) {
        Log("history not supported");
        window.location.href = url;
        return;
    }
    history.pushState(null, null, url);
}

function UpdateUrl(url) {
    if (!window._isHistorySupported) {
        return;
    }
    window.history.replaceState(null, "", url);
}

// outputs an error message to the browser console, if it has one.
function Log(error) {
    if (this.console) {
        console.log(error);
    }
}

function GetShortDate(date) {
    return window.moment(date).format("MMM Do YY");
}

function GetShortDateTime(date) {
    return window.moment(date).format("MMM Do YYYY, h:mm a");
}

function GetRelativeDate(date) {
    return window.moment(date).fromNow();
}

function DateDiff(date1, date2) {
    var dateDiff = date1.getTime() - date2.getTime(); //store the getTime diff - or +
    return (dateDiff / (24 * 60 * 60 * 1000)); //Convert values to -/+ days and return value      
}

function MinuteDiff(date1, date2) {
    var minuteDiff = date1.getTime() - date2.getTime(); //store the getTime diff - or +
    return (minuteDiff / (60 * 1000)); //Convert values to -/+ minutes and return value      
}

// determines whether the client device is a desktop or phone by looking at screen resolution
function GetDeviceType() {
    var bigAxis = screen.width > screen.height ? screen.width : screen.height;
    // @Michael credit for innerWidth 420 addition
    if (bigAxis >= 960 && (window.innerWidth || 420) >= 420) {
        return "desktop";
    } else {
        return "phone";
    }
}

function GetOperatingSystem() {
    var osName = "Unknown OS";
    if (navigator.appVersion.indexOf("Win") !== -1) osName = "Windows";
    if (navigator.appVersion.indexOf("Mac") !== -1) osName = "MacOS";
    if (navigator.appVersion.indexOf("X11") !== -1) osName = "UNIX";
    if (navigator.appVersion.indexOf("Linux") !== -1) osName = "Linux";
    return osName;
}

// determines whether or not the client is using a common mobile device as determined by the client's operating system. 
function IsMobile() {
    var isMobile = /(iPhone|iPod|iPad|BlackBerry|BB10|Android|MeeGo|KFAPWI)/.test(navigator.userAgent);
    return isMobile;
}

// determines whether or not the client is using an Apple iOS device
function IsIos() {
    return /(iPhone|iPod|iPad)/.test(navigator.userAgent);
}

// returns the pixel density ratio of the clients display.
// this will be 1 for regular desktop monitors and higher values like 2 for high-density screens like the Apple Retina screen.
function GetPixelRatio() {
    var pixelRatio = 1;
    if (window.devicePixelRatio != undefined) {
        pixelRatio = window.devicePixelRatio;
    }
    return pixelRatio;
}

function GetPhotoUrl(fileStoreId, width, height) {
    if (height === undefined) {
        return "/os/" + fileStoreId + "?maxwidth=" + width + "&zoom=" + window.GetPixelRatio();
    } else {
        return "/os/" + fileStoreId + "?maxwidth=" + width + "&maxheight=" + height + "&zoom=" + window.GetPixelRatio();
    }
}

function GetCoverPhotoUrl(fileStoreId, width, height) {
    if (width != null) {
        return "/os/" + fileStoreId + "?maxwidth=" + width + "&c=Covers&zoom=" + window.GetPixelRatio();
    } else {
        return "/os/" + fileStoreId + "?maxheight=" + height + "&c=Covers&zoom=" + window.GetPixelRatio();
    }
}

function GetIntercomPhotoUrl(fileStoreId, width, height) {
    if (width != null) {
        return "/os/" + fileStoreId + "?maxwidth=" + width + "&c=PrivateMessagePhotos&zoom=" + window.GetPixelRatio();
    } else {
        return "/os/" + fileStoreId + "?maxheight=" + height + "&c=PrivateMessagePhotos&zoom=" + window.GetPixelRatio();
    }
}

function GetProfilePhotoUrl(fileStoreId, size) {
    if (window.IsNullOrEmpty(fileStoreId)) {
        return "/content/images/helmet.png";
    }
    return "/os/" + fileStoreId + "?w=" + size + "&h=" + size + "&c=Profiles&mode=crop&zoom=" + window.GetPixelRatio();
}

// returns HTML for a users profile photo. if the user has uploaded one this is shown, otherwise an initial for the username is shown
function GetUserProfileGraphic(size, username, profileFileStoreId) {

    if (IsNullOrEmpty(username)) {
        return null;
    }

    var sizePx;
    switch (size) {
        case "small":
            sizePx = 25;
            break;
        case "header":
            sizePx = 30;
            break;
        case "medium":
        default:
            sizePx = 50;
            break;
    }

    if (!IsNullOrEmpty(profileFileStoreId)) {

        // build html that shows the users uploaded profile photo
        var url = GetProfilePhotoUrl(profileFileStoreId, sizePx);
        return "<img src=\"" + url + "\" class=\"img-circle img-profile-photo\" style=\"width: " + sizePx + "px; height: " + sizePx + "px;\"/>";

    } else {

        // build html that shows the users initial and a varied colour
        // round up as some device pixel ratios are decimals
        _lastUsedProfilePhotoInitialVariant = 1;
        
        return "<div class=\"avatar-" + size + " avatar-" + _lastUsedProfilePhotoInitialVariant + " img-circle img-profile-photo\">" + username.substring(0, 1).toUpperCase() + "</div>";
    }
}

// determines what name template should be used for the text editor control depending on what device is being used
function GetTextEditorTemplate() {

    // leaving this commented out. iOS performance just isn't quite there. it's a little buggy.

    // android devices seem to have number of issues with Redactor (or visa-versa!)
    // so only iOS and desktop users can use the Redactor editor, though iOS users will see it without the toolbar.
    //if (IsIos() || !IsMobile()) {
    //    return "text-editor-desktop";
    //}

    // everyone else gets the plain-text version
    //return "text-editor-mobile";

    return IsMobile() ? "text-editor-mobile" : "text-editor-desktop";
}

function GetUserProfileUrl(username) {
    if (IsNullOrEmpty(username)) {
        return null;
    }

    // we use a custom form of URL encoding for usernames as there's a lot of users with spaces in their names
    // and we don't want URLs to look like londonbikers.com/750%20man
    var url = "/" + encodeURIComponent(username);
    url = url.replace(/%20/g, "+");

    if (username.indexOf(".") > -1) {
        url += "/";
    }
    return url.toLowerCase();
}

function IsElementInViewport(element) {

    //special bonus for those using jQuery
    if (typeof jQuery === "function" && element instanceof jQuery) {
        element = element[0];
    }

    var rect = element.getBoundingClientRect();

    return (
        rect.top >= 0 &&
        rect.left >= 0 &&
        rect.bottom <= (window.innerHeight || document.documentElement.clientHeight) && /*or $(window).height() */
        rect.right <= (window.innerWidth || document.documentElement.clientWidth) /*or $(window).width() */
    );
}

function GetTopicIconsHtml(topicHeader) {
    var html = "";

    if (topicHeader.IsNew()) {
        html += "<span class=\"label label-danger\">new</span>";
    } else if (topicHeader.HasNewReplies()) {
        html += "<span class=\"label label-info\">updated</span>";
    }

    return html;
}

function GetTopicAdditionalIconsHtml(topicHeader) {
    var html = "";

    if (topicHeader.IsSticky()) {
        html += "<span class=\"glyphicon glyphicon-pushpin\" aria-hidden=\"true\" title=\"Pinned\"></span>";
    }

    if (topicHeader.StatusCode() === 2) {
        if (!window.IsNullOrEmpty(html)) {
            html += "<span class=\"glyphicon glyphicon-lock ml5\" aria-hidden=\"true\" title=\"Replies locked\"></span>";
        } else {
            html += "<span class=\"glyphicon glyphicon-lock\" aria-hidden=\"true\" title=\"Replies locked\"></span>";
        }
    }

    if (topicHeader.HasPhotos() || topicHeader.HasAttachments()) {
        if (!window.IsNullOrEmpty(html)) {
            html += "<span class=\"glyphicon glyphicon-picture ml5\" aria-hidden=\"true\" title=\"Has photos\"></span>";
        } else {
            html += "<span class=\"glyphicon glyphicon-picture\" aria-hidden=\"true\" title=\"Has photos\"></span>";
        }
    }

    return html;
}

(function ($) {
    $.fn.goTo = function (callback, accomodateTopBar) {
        try {
            if ($(this) == null || typeof $(this) === "undefined") {
                return this;
            }

            // the top bar takes up an amount of the viewport, without accommodating for it items can be scrolled so they're still underneath it
            var offset = 0;
            var elementTop = $(this).offset().top;
            if (accomodateTopBar) {
                offset = $("header").outerHeight();
            }

            if (callback != null) {
                $("html, body").animate({ scrollTop: (elementTop - offset) }, "slow", callback(this));
            } else {
                $("html, body").animate({ scrollTop: (elementTop - offset) }, "slow");
            }

            return this; // for chaining...
        } catch (error) {
            Log(error);
            return this;
        }
    }
})(jQuery);

// arbitrarily dependent upon current design (obviously not ideal)
function GetEditModalEditorMaxHeight(isTopic) {

    var editorTop = 102;
    var modalFooter = 95;
    var modalMarginBottom = 20;
    var misc = 60;
    var result = $(window).height() - editorTop - modalFooter - modalMarginBottom - misc;

    // topics have a subject field taking up space too
    if (isTopic) {
        result = result - 44;
    }

    return result;
}

function IsContentHtml(content) {
    var regex = new RegExp(/<[a-z][\s\S]*>/);
    var isHtml = regex.test(content);
    return isHtml;
}

function HtmlToText(html) {

    if (IsNullOrEmpty(html)) {
        return null;
    }

    html = html.replace(/<style([\s\S]*?)<\/style>/gi, "");
    html = html.replace(/<script([\s\S]*?)<\/script>/gi, "");
    html = html.replace(/<\/div>/ig, "\n");
    html = html.replace(/<\/li>/ig, "\n");
    html = html.replace(/<li>/ig, "  *  ");
    html = html.replace(/<\/ul>/ig, "\n");
    html = html.replace(/<\/p>/ig, "\n");
    html = html.replace(/<br\s*[\/]?>/gi, "\n");
    html = html.replace(/<[^>]+>/ig, "");
    html = html.replace(/\t/gmi, "");
    html = html.trim();

    return html;
}

// used as the sort function for private message headers.
function SortPrivateMessageHeaders(a, b) {
    if (a.LastMessageCreated().getTime() > b.LastMessageCreated().getTime()) {
        return -1;
    } else if (a.LastMessageCreated().getTime() === b.LastMessageCreated().getTime()) {
        return 0;
    } else {
        return 1;
    }
}

// used as the sort function for forum header lists.
function SortTopicHeaders(a, b) {

    if (a.IsSticky() && b.IsSticky()) {

        //Log("branch 1");

        if (a.Updated() > b.Updated()) {
            return -1;
        } else if (a.Updated() === b.Updated()) {
            return 0;
        } else {
            return 1;
        }

    } else if (!a.IsSticky() && !b.IsSticky()) {

        //Log("branch 2");

        if (a.Updated() > b.Updated()) {
            //Log("SortTopicHeaders: a > b");
            return -1;
        } else if (a.Updated() === b.Updated()) {
            //Log("SortTopicHeaders: a === b");
            return 0;
        } else {
            //Log("SortTopicHeaders: a < b");
            return 1;
        }

    } else {

        //Log("branch 3");
        
        // one is sticky, the other is not
        if (a.IsSticky() && !b.IsSticky()) {
            return -1;
        } else {
            return 1;
        }

    }
}

// used as the sort function for forum header lists.
function SortTopicHeadersByDate(a, b) {
    if (a.Updated() > b.Updated()) {
        return -1;
    } else if (a.Updated() === b.Updated()) {
        return 0;
    } else {
        return 1;
    }
}

function GetForumUrl(forumId, forumName) {
    return "/forums/" + forumId + "/" + EncodeUrlPart(forumName);
}

// plain JavaScript function to determine if an element resides under another.
function isDescendant(parent, child) {
    var node = child.parentNode;
    while (node != null) {
        if (node === parent) {
            return true;
        }
        node = node.parentNode;
    }
    return false;
}

/**
 * Used to start the Signalr real-time web hub - without any callbacks.
 * Used on views without Signalr needs themselves but where other hub actions are needed, i.e. the notification menus.
 * @returns {} 
 */
function StartHub() {

    Log("StartHub()");
    $.connection.hub.start().done(function () {
        // see if there's any post hub-start tasks that need executing
        if (!IsNull(_postHubStartTasks)) {
            _postHubStartTasks.forEach(function (task) {
                Log("Running a post hub-start task...");
                task();
            });
        }
    });

}

function GetStatusName(statusNumber) {
    switch (statusNumber) {
        case 0:
            return "Active";
        case 1:
            return "Suspended";
        case 2:
            return "Banned";
    }
    return "Unknown";
}

function MarkPhotoViewed(photoId) {
    $.ajax({
        url: "/api/photos/PhotoViewed?photoId=" + photoId,
        type: "PUT",
        success: function () {
            Log("Marked photo as viewed: " + photoId);
        }
    });
}

/**[ GALLERIES ]************************************************/

function IsCategoryAGallery(categoryId) {
    var category = window._categories.find(function (c) {
        return parseInt(c.Id) === parseInt(categoryId);
    });

    return category.IsGalleryCategory;
}

function IsForumAGallery(forumId) {
    var category = window._categories.find(function (c) {
        var forum = c.Forums.find(function (f) {
            return parseInt(f.Id) === parseInt(forumId);
        });
        return forum != undefined;
    });

    return category.IsGalleryCategory;
}