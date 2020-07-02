using System.Web.Http;
using System.Web.Http.ExceptionHandling;

namespace LBV6
{
    public static class WebApiConfig
    {
        #region accessors
        public static string UrlPrefix => "api";
        public static string UrlPrefixRelative => "~/api";
        #endregion

        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            // manual routes created so we can have an API naming convention that is similar to page controllers.
            // would rather not have to specify them manually - is there a way to work out if we can append "Api" on to the end of the controller token?
            config.Routes.MapHttpRoute("Categories API", UrlPrefix + "/categories/{id}", new { controller = "CategoriesApi",id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Forums API", UrlPrefix + "/forums/{action}/{id}", new { controller = "ForumsApi", action = RouteParameter.Optional, id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Legacy API", UrlPrefix + "/legacy/{id}", new { controller = "LegacyApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Replies API", UrlPrefix + "/replies/{id}", new { controller = "RepliesApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Topics API", UrlPrefix + "/topics/{action}/{id}", new { controller = "TopicsApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Users API", UrlPrefix + "/users/{action}/{id}", new { controller = "UsersApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Intercom API", UrlPrefix + "/intercom/{action}/{id}", new { controller = "IntercomApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Photos API", UrlPrefix + "/photos/{action}/{id}", new { controller = "PhotosApi", id = RouteParameter.Optional });
            config.Routes.MapHttpRoute("Notifications API", UrlPrefix + "/notifications/{action}/{id}", new { controller = "NotificationsApi", id = RouteParameter.Optional });

            config.Services.Add(typeof(IExceptionLogger), new AiExceptionLogger());
        }
    }
}