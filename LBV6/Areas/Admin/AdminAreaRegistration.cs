using System.Web.Mvc;

namespace LBV6.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration 
    {
        public override string AreaName => "Admin";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("AdminStructure", "admin/structure/{action}/{id}", new { controller = "AdminStructure", action = "Index", id = UrlParameter.Optional });
            context.MapRoute("AdminMigration", "admin/migration/{action}/{id}", new { controller = "AdminMigration", action = "Index", id = UrlParameter.Optional });
            context.MapRoute("AdminUsers", "admin/users/{action}/{id}", new { controller = "AdminUsers", action = "Index", id = UrlParameter.Optional });
            
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{id}",
                new { controller = "AdminHome", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}