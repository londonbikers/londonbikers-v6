using LBV6ForumApp;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace LBV6
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("robots.txt");
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.LowercaseUrls = true;

            routes.MapRoute("Getting Started", "getting-started", new { controller = "Home", action = "GettingStarted" });
            routes.MapRoute("Rules", "rules", new { controller = "Home", action = "Rules" });
            routes.MapRoute("Contact", "contact", new { controller = "Home", action = "Contact" });
            routes.MapRoute("Change", "change", new { controller = "Home", action = "Change" });
            routes.MapRoute("Error", "error", new { controller = "Home", action = "Error" });
            routes.MapRoute("ErrorTest", "errortest", new { controller = "Home", action = "ErrorTest" });
            routes.MapRoute("NotFound", "notfound", new { controller = "Home", action = "NotFound" });
            routes.MapRoute("Search", "search", new { controller = "Search", action = "Index" });
            routes.MapRoute("Privacy", "privacy", new { controller = "Home", action = "Privacy" });

            // we need to explicitly define routes for all other pages otherwise the user profile route
            // will result in a user lookup for all other URLs which will impact performance
            routes.MapRoute("Intercom", "intercom", new { controller = "Intercom", action = "Index" });
            routes.MapRoute("Manage", "manage", new { controller = "Manage", action = "Index" });

            routes.MapRoute(
                name: "User Profiles",
                url: "{username}",
                defaults: new { controller = "Users", action = "UserProfile" },
                constraints: new { username = new UserConstraint() }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }

    public class UserConstraint : IRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName, RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (!parameterName.Equals("username"))
                return false;

            // we use a custom form of url encoding for usernames as there's a lot of users with spaces in their names
            // and we don't want url's to look like londonbikers.com/750%20man
            var username = values["username"].ToString().Replace("+", " ");
            var user = ForumServer.Instance.Users.GetUserByUsernameAsync(username).Result;
            return user != null;
        }
    }
}