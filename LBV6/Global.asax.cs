using LBV6.Models;
using LBV6ForumApp;
using LBV6Library;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNet.Identity;
using StackExchange.Profiling;
using StackExchange.Profiling.EntityFramework6;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.SessionState;

namespace LBV6
{
    public class MvcApplication : HttpApplication
    {
        #region members
        private readonly TelemetryClient _telemetry;
        #endregion

        #region constructors
        public MvcApplication()
        {
            _telemetry = new TelemetryClient();
        }
        #endregion

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            MiniProfiler.Settings.Results_Authorize = IsUserAllowedToSeeMiniProfilerUi;
            MiniProfiler.Settings.Results_List_Authorize = IsUserAllowedToSeeMiniProfilerUi;
            MiniProfiler.Settings.PopupRenderPosition = RenderPosition.BottomLeft;
            MiniProfilerEF6.Initialize();

            Logging.LogInfo(GetType().FullName, "Starting the LBV6 site");

            TelemetryConfiguration.Active.InstrumentationKey = ConfigurationManager.AppSettings["ApplicationInsights.InstrumentationKey"];
            HttpContext.Current.Application.Lock();
            HttpContext.Current.Application["Telemetry"] = _telemetry;
            HttpContext.Current.Application.UnLock();

            // don't advertise we're running MVC
            MvcHandler.DisableMvcResponseHeader = true;
        }

        protected void Application_PostAuthorizeRequest()
        {
            if (IsWebApiRequest())
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
        }

        protected void Application_BeginRequest()
        {
            var redirect = RedirectLegacyUrls(Request.Url.AbsoluteUri, Request.Path, Request.IsSecureConnection, Request.QueryString);
            if (redirect.HasValue && redirect.Value.Type == RedirectType.Temporary)
                Response.Redirect(redirect.Value.Url);
            else if (redirect.HasValue && redirect.Value.Type == RedirectType.Permanent)
                Response.RedirectPermanent(redirect.Value.Url);

            if (Request.IsLocal)
                MiniProfiler.Start();
        }

        protected void Application_EndRequest()
        {
            if (Request.IsLocal)
                MiniProfiler.Stop();
        }

        protected void Application_End()
        {
            Logging.LogInfo(GetType().FullName, "Shutting the LBV6 site down");
            ForumServer.Instance.Dispose();
        }

        protected void Session_Start()
        {
            if (!Request.IsAuthenticated)
                return;

            // user has auto-authenticated, i.e. is being remembered.
            var userId = User.Identity.GetUserId();
            var user = ForumServer.Instance.Users.GetUser(userId);

            // temporary debug code to try and nail down a session-set-up issue
            if (user == null)
                Logging.LogError(GetType().FullName, "Couldn't retrieve user with id: " + userId);
            else
            {
                Helpers.LogUserVisit(user);
                Helpers.Telemetry.TrackEvent("Auto login", new Dictionary<string, string> { { "Username", user.UserName } });
            }
        }

        protected void Session_End()
        {
            // it does get called!
            Logging.LogDebug(GetType().FullName, "Ran.");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // get the exception object
            var exception = Server.GetLastError();

            // log the error to Application Insights
            var ai = new TelemetryClient();
            ai.TrackException(exception);

            // log the error to Log4Net
            Logging.LogPageError(GetType().FullName, Request.Url.AbsoluteUri, exception);
        }

        #region private methods
        private static bool IsWebApiRequest()
        {
            return HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath != null && HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath.StartsWith(WebApiConfig.UrlPrefixRelative);
        }

        private static bool IsUserAllowedToSeeMiniProfilerUi(HttpRequest httpRequest)
        {
            return httpRequest.IsLocal;
        }

        /// <summary>
        /// Redirects legacy URLs and ensures https is used for new pages.
        /// </summary>
        /// <remarks>
        /// Don't be tempted to refactor this for ASYNC support! It doesn't work under the calling method Application_BeginRequest()'s scope.
        /// </remarks>
        public Redirect? RedirectLegacyUrls(string absoluteUri, string path, bool isSecureConnection, NameValueCollection queryString)
        {
            // if it's legacy forum topic URL, perm redirect it
            // if it's a legacy content URL, temp redirect it
            // if it's a new page being requested over http, change it to https (this might be difficult)

            if (Regex.IsMatch(path, @"/forums/Topic(.*?)\.aspx", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                return RedirectLegacyTopicShortFormUrl(path);

            if (path.StartsWith("/forums/recent", StringComparison.CurrentCultureIgnoreCase))
                return RedirectLegacyRecentUrl(path);

            if (Regex.IsMatch(path, @"/forums/posts/(\d*)/?$", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                return RedirectLegacyPostUrl(path);

            if (!isSecureConnection && Regex.IsMatch(path, @"/forums/(\d+?)/.+", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                return RedirectLegacyTopicUrl(path, queryString);

            if (Regex.IsMatch(path, @"/forums/PrintTopic(.*?)\.aspx", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                return RedirectLegacyPrintTopicUrl(path);

            if (Regex.IsMatch(path, @"/forums/FindPost(.*?)\.aspx", RegexOptions.Compiled | RegexOptions.IgnoreCase))
                return RedirectLegacyFindPostUrl(path);

            if (path.StartsWith("/forums/calendar", StringComparison.CurrentCultureIgnoreCase))
                return RedirectPermanentlyUnsupportedUrl(path);

            var tempUnsupportedPaths = new List<string> { "/news", "/features", "/articles", "/galleries", "/store", "/competitions", "/charity", "/advertising" };
            if (tempUnsupportedPaths.Any(lcp => path.StartsWith(lcp, StringComparison.CurrentCultureIgnoreCase)))
                return RedirectTemporarilyUnsupportedUrl(path);

            var permUnsupportedPaths = new List<string> { "/events", "/directory", "/partners", "/tags", "/blog", "/tv", "/story.aspx", "/bsb", "/motogp", "/podcast", "/technology", "/products", "/forums/rssfeed", "/insurance" };
            if (permUnsupportedPaths.Any(lcp => path.StartsWith(lcp, StringComparison.CurrentCultureIgnoreCase)))
                return RedirectPermanentlyUnsupportedUrl(path);

            if (path.StartsWith("/img.ashx", StringComparison.CurrentCultureIgnoreCase))
                return RedirectTemporarilyLegacyImageHandlerUrl(path);

            if (path.StartsWith("/forums/uploads/avatars/", StringComparison.CurrentCultureIgnoreCase))
                return RedirectPermanentlyLegacyImageHandlerUrl(path);

            if (path.StartsWith("/feed.ashx", StringComparison.CurrentCultureIgnoreCase))
                return RedirectLegacyNonConvertibleUrl(path);

            if (path.StartsWith("/forums/privatemessage", StringComparison.InvariantCultureIgnoreCase))
                return RedirectLegacyNonConvertibleUrl(path);

            if (path.StartsWith("/login", StringComparison.CurrentCultureIgnoreCase))
                return RedirectLegacyLoginUrl(path);

            if (path.StartsWith("/register", StringComparison.CurrentCultureIgnoreCase))
                return RedirectLegacyRegisterUrl(path);

            if (path.StartsWith("/forums/latest", StringComparison.CurrentCultureIgnoreCase))
                return RedirectForumsLatestUrl(path);

            // all non-https URLs apart from the Let's Encrypt authorisation URL should get redirected
            if (!isSecureConnection && !path.StartsWith("/.well-known/"))
                return RedirectLegacyMiscUrl(path, absoluteUri);

            // not a URL we need to redirect. Do nothing.
            return null;
        }
        #endregion

        #region legacy redirects
        private Redirect? RedirectLegacyTopicShortFormUrl(string path)
        {
            // i.e. /forums/Topic812033.aspx
            // i.e. /forums/topic760445-44-1.aspx
            var matches = Regex.Match(path, @"Topic(.+?)[-|\.]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!matches.Success)
            {
                _telemetry.TrackEvent("RedirectLegacyTopicShortFormUrl: Legacy post id couldn't be found in URL", new Dictionary<string, string> { { "URL", path } });
                return null;
            }

            var legacyPostId = int.Parse(matches.Groups[1].Value);
            var topic = ForumServer.Instance.Posts.GetTopicByLegacyId(legacyPostId);
            if (topic == null)
            {
                Logging.LogError(GetType().FullName, $"No such topic: {legacyPostId}");
                _telemetry.TrackEvent("RedirectLegacyTopicShortFormUrl: No such topic", new Dictionary<string, string> { { "URL", path } });
                return new Redirect { Type = RedirectType.Permanent, Url = ConfigurationManager.AppSettings["LB.Url"] };
            }

            _telemetry.TrackEvent("Legacy Forum Post Topic Redirect (Topic short format)", new Dictionary<string, string> { { "URL", path } });
            return new Redirect { Type = RedirectType.Permanent, Url = Urls.GetTopicUrl(topic, true) };
        }

        private Redirect RedirectLegacyRecentUrl(string path)
        {
            _telemetry.TrackEvent("Legacy Forum Recent Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/forums/latest";
            return new Redirect { Type = RedirectType.Permanent, Url = url };
        }

        private Redirect? RedirectLegacyPostUrl(string path)
        {
            // seems to be a legacy forum topic URL. there's a couple of variants, one for topics and one for replies:
            // http://londonbikers.com/forums/posts/505798

            var parts = path.Split('/');
            var postId = int.Parse(parts[3]);

            var container = ForumServer.Instance.Posts.GetReplyByLegacyId(postId);
            if (container != null)
            {
                #region specific post - reply
                // reply - work out
                var topic = ForumServer.Instance.Posts.GetTopic(container.TopicId);
                var replyPosition = topic.Replies.FindIndex(q => q.Id.Equals(container.ReplyId));
                var pageSize = int.Parse(ConfigurationManager.AppSettings["LB.DefaultPageSize"]);
                var page = 1;
                var pageItemCount = 1;

                for (var i = 0; i < replyPosition; i++)
                {
                    if (pageItemCount == pageSize)
                    {
                        page++;
                        pageItemCount = 1;
                        continue;
                    }

                    pageItemCount++;
                }

                string topicUrl;
                if (page > 1)
                    topicUrl = Urls.GetTopicUrl(topic, true) + "?p=" + page + "&hid=" + container.ReplyId;
                else
                    topicUrl = Urls.GetTopicUrl(topic, true) + "?hid=" + container.ReplyId;

                _telemetry.TrackEvent("Legacy Forum Post Reply Redirect", new Dictionary<string, string> { { "URL", path } });
                return new Redirect { Type = RedirectType.Permanent, Url = topicUrl };
                #endregion
            }
            else
            {
                #region specific post - topic
                // post is not a reply, try looking for a topic with that id
                var topic = ForumServer.Instance.Posts.GetTopicByLegacyId(postId);
                if (topic != null)
                {
                    _telemetry.TrackEvent("Legacy Forum Post Topic Redirect", new Dictionary<string, string> { { "URL", path } });
                    return new Redirect { Type = RedirectType.Permanent, Url = Urls.GetTopicUrl(topic, true) };
                }

                _telemetry.TrackEvent("Legacy Post Not Found", new Dictionary<string, string> { { "PostId", postId.ToString() } });
                #endregion
            }

            return null;
        }

        /// <summary>
        /// Redirects a legacy topic URL to a new one.
        /// </summary>
        /// <param name="path">To be sourced from Request.Path</param>
        /// <param name="queryString">To be sourced from Request.QueryString</param>
        private Redirect? RedirectLegacyTopicUrl(string path, NameValueCollection queryString)
        {
            // legacy forum topic URL. There's a couple of variants, one for topics and one for replies:
            // http://londonbikers.com/forums/1101459/time-for-a-reboot-relaunching-lb

            var parts = path.Split('/');
            if (!int.TryParse(parts[2], out var topicId))
                return null;

            var topic = ForumServer.Instance.Posts.GetTopicByLegacyId(topicId);
            if (topic == null)
            {
                _telemetry.TrackEvent("Legacy Topic Not Found", new Dictionary<string, string> { { "URL", path } });
                var url = ConfigurationManager.AppSettings["LB.Url"];
                return new Redirect { Type = RedirectType.Permanent, Url = url };
            }

            var topicUrl = Urls.GetTopicUrl(topic, true);
            if (queryString["PageIndex"] != null && topic.Replies.Count > 25)
            {
                // old LB had 20 items per page, new one has 25, so recalculate the page number
                var pageNumber = int.Parse(queryString["PageIndex"]);
                pageNumber = Convert.ToInt32(pageNumber * 0.8);
                if (pageNumber > 1)
                    topicUrl += "?p=" + pageNumber;
            }

            _telemetry.TrackEvent("Legacy Topic Redirect", new Dictionary<string, string> { { "URL", path } });
            return new Redirect { Type = RedirectType.Permanent, Url = topicUrl };
        }

        private Redirect? RedirectLegacyPrintTopicUrl(string path)
        {
            // legacy forum print topic URL, i.e. http://londonbikers.com/forums/printtopic774555.aspx
            var matches = Regex.Match(path, @"printtopic(.+?)[-|\.]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!matches.Success)
            {
                _telemetry.TrackEvent("RedirectLegacyPrintTopicUrl: Legacy pritn topic id couldn't be found in URL", new Dictionary<string, string> { { "URL", path } });
                return null;
            }

            var legacyPostId = int.Parse(matches.Groups[1].Value);
            var topic = ForumServer.Instance.Posts.GetTopicByLegacyId(legacyPostId);
            if (topic == null)
            {
                _telemetry.TrackEvent("Legacy Topic Not Found", new Dictionary<string, string> { { "URL", path } });
                var url = ConfigurationManager.AppSettings["LB.Url"];
                return new Redirect { Type = RedirectType.Permanent, Url = url };
            }

            var topicUrl = Urls.GetTopicUrl(topic, true);

            _telemetry.TrackEvent("Legacy Print Topic Redirect", new Dictionary<string, string> { { "URL", path } });
            return new Redirect { Type = RedirectType.Permanent, Url = topicUrl };
        }

        private Redirect? RedirectLegacyFindPostUrl(string path)
        {
            // i.e. /forums/FindPost742650.aspx
            var matches = Regex.Match(path, @"FindPost(.+?)[-|\.]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!matches.Success)
            {
                _telemetry.TrackEvent("RedirectLegacyFindPostUrl: Legacy post id couldn't be found in URL", new Dictionary<string, string> { { "URL", path } });
                return null;
            }

            var legacyPostId = int.Parse(matches.Groups[1].Value);
            var topic = ForumServer.Instance.Posts.GetTopicByLegacyId(legacyPostId);

            if (topic != null)
            {
                _telemetry.TrackEvent("Legacy Forum Find Post Redirect (topic)", new Dictionary<string, string> { { "URL", path } });
                return new Redirect { Type = RedirectType.Permanent, Url = Urls.GetTopicUrl(topic, true) };
            }

  
            // no topic found with this legacy post id.
            // try and find a reply instead.
            var reply = ForumServer.Instance.Posts.GetReplyByLegacyId(legacyPostId);
            if (reply != null)
            {
                topic = ForumServer.Instance.Posts.GetTopic(reply.TopicId);
                if (topic != null)
                {
                    _telemetry.TrackEvent("Legacy Forum Find Post Redirect (reply)", new Dictionary<string, string> { { "URL", path } });
                    return new Redirect { Type = RedirectType.Permanent, Url = Urls.GetTopicUrl(topic, true) };
                }
            }

            _telemetry.TrackEvent("RedirectLegacyFindPostUrl: No such post", new Dictionary<string, string> { { "URL", path } });
            return new Redirect { Type = RedirectType.Permanent, Url = ConfigurationManager.AppSettings["LB.Url"] };
        }

        private Redirect RedirectTemporarilyUnsupportedUrl(string path)
        {
            // seems to be a legacy content URL, i.e.
            // http://londonbikers.com/news

            // perform a temporary (302) redirect to our page explaining why we don't have this content at the moment.
            _telemetry.TrackEvent("Legacy URL Temp Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/change";
            return new Redirect { Type = RedirectType.Temporary, Url = url };
        }

        private Redirect RedirectPermanentlyUnsupportedUrl(string path)
        {
            // seems to be a legacy content URL, i.e.
            // http://londonbikers.com/directory

            // perform a temporary (302) redirect to our page explaining why we don't have this content at the moment.
            _telemetry.TrackEvent("Legacy URL Temp Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/change";
            return new Redirect { Type = RedirectType.Permanent, Url = url };
        }

        private Redirect RedirectTemporarilyLegacyImageHandlerUrl(string path)
        {
            // perform a temporary (302) redirect to our image-not-available placeholder
            _telemetry.TrackEvent("Legacy URL Temp Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/content/images/lb-image-not-available.png";
            return new Redirect { Type = RedirectType.Temporary, Url = url };
        }

        private Redirect RedirectPermanentlyLegacyImageHandlerUrl(string path)
        {
            // perform a temporary (302) redirect to our image-not-available placeholder
            _telemetry.TrackEvent("Legacy URL Temp Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/content/images/lb-image-not-available.png";
            return new Redirect { Type = RedirectType.Permanent, Url = url };
        }

        private Redirect RedirectLegacyNonConvertibleUrl(string path)
        {
            _telemetry.TrackEvent("Legacy Non-Convertible URL", new Dictionary<string, string> { { "URL", path } });
            return new Redirect { Type = RedirectType.Permanent, Url = ConfigurationManager.AppSettings["LB.Url"] };
        }

        private Redirect RedirectLegacyLoginUrl(string path)
        {
            _telemetry.TrackEvent("Legacy Sign-In URL Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/account/sign-in";
            return new Redirect { Type = RedirectType.Permanent, Url = url };
        }

        private Redirect RedirectLegacyRegisterUrl(string path)
        {
            _telemetry.TrackEvent("Legacy Register URL Redirect", new Dictionary<string, string> { { "URL", path } });
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/account/register";
            return new Redirect { Type = RedirectType.Permanent, Url = url };
        }

        /// <summary>
        /// Redirects a generic URL to a new one, i.e. to it's HTTPS equivilent.
        /// </summary>
        /// <param name="path">To be sourced from Request.Path</param>
        /// <param name="absoluteUri">To be sourced from Request.Url.AbsoluteUri</param>
        private Redirect RedirectLegacyMiscUrl(string path, string absoluteUri)
        {
            // content is some other http page that we can just change to https.
            _telemetry.TrackEvent("Legacy HTTP URL Redirect", new Dictionary<string, string> { { "URL", path } });
            var sourceUri = new Uri(absoluteUri.ToLowerInvariant());
            var newUrl = ConfigurationManager.AppSettings["LB.Url"] + sourceUri.AbsolutePath;
            return new Redirect { Type = RedirectType.Permanent, Url = newUrl };
        }

        public Redirect RedirectForumsLatestUrl(string path)
        {
            // redirecting /forums/latest to /forums
            _telemetry.TrackEvent("Forums Latest URL Redirect", new Dictionary<string, string> { { "URL", path } });
            var newUrl = ConfigurationManager.AppSettings["LB.Url"] + "/forums";
            return new Redirect { Type = RedirectType.Permanent, Url = newUrl };
        }
    }
    #endregion
}