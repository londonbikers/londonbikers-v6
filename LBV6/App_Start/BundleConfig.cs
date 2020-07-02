using System.Web.Optimization;

namespace LBV6
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include("~/scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/head").Include("~/scripts/modernizr-custom.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/scripts/bootstrap.js",
                "~/scripts/respond.js",
                "~/scripts/bootstrap-toggle.js"));

            bundles.Add(new ScriptBundle("~/bundles/common").Include(
                "~/scripts/bootstrap.js",
                "~/scripts/bootstrap-select.js",
                "~/scripts/respond.js",
                "~/scripts/bootstrap-toggle.js",
                "~/scripts/knockout-{version}.js",
                //"~/scripts/knockout-{version}.debug.js",
                "~/scripts/underscore.js",
                "~/scripts/ko.datasource.js",
                "~/scripts/knockout-js-infinite-scroll.js",
                "~/scripts/knockout-redactor.js",
                "~/scripts/miniNotification.js",
                "~/scripts/moment.js",
                "~/scripts/jquery.cookie.js",
                "~/scripts/dropzone/dropzone.js",
                "~/scripts/jquery.popup.js",
                "~/scripts/jquery.waitforimages.js",
                "~/scripts/redactor.js",
                "~/scripts/redactor-source.js",
                "~/scripts/redactor-quote.js",
                "~/scripts/PhotoSwipe/*.js",
                "~/scripts/lightgallery.js",
                "~/scripts/lg-hash.js",
                "~/scripts/Common/Text.js",
                "~/scripts/Common/Templates.js",
                "~/scripts/Common/CommonLib.js",
                "~/scripts/Common/KnockoutLib.js",
                "~/scripts/Common/PhotoLib.js",
                "~/scripts/Common/PhotoSwipeLib.js",
                "~/scripts/Models/*.js",
                "~/scripts/GlobalViewModels/*.js",
                "~/scripts/jquery.signalR-{version}.js"));

            // admin specific
            bundles.Add(new ScriptBundle("~/bundles/adminjs").Include(
                "~/scripts/jquery-ui-{version}.js",
                "~/scripts/knockout-sortable.js",
                "~/scripts/admin/models/*.js"));

            bundles.Add(new StyleBundle("~/content/admincss").Include(
                "~/content/bootstrap.css",
                "~/content/bootstrap-select.css",
                "~/content/jquery.mininotification.css",
                "~/content/adminsite.css"));

            bundles.Add(new LessBundle("~/content/adminless").Include("~/Content/bootstrap-toggle.less"));

            // per-page bundles
            bundles.Add(new ScriptBundle("~/bundles/_shared").Include("~/scripts/views/_shared.js"));
            bundles.Add(new ScriptBundle("~/bundles/splash").Include("~/scripts/views/splash.js"));
            bundles.Add(new ScriptBundle("~/bundles/profile").Include("~/scripts/views/profile.js"));
            bundles.Add(new ScriptBundle("~/bundles/intercom").Include("~/scripts/views/intercom.js"));
            bundles.Add(new ScriptBundle("~/bundles/manage").Include("~/scripts/views/manage.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/index").Include("~/scripts/forums/index.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/category").Include("~/scripts/forums/category.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/forum").Include("~/scripts/forums/forum.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/popular_topics").Include("~/scripts/forums/popular.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/latest_topics").Include("~/scripts/forums/latest.js"));
            bundles.Add(new ScriptBundle("~/bundles/forums/topic").Include("~/scripts/forums/topic.js", "~/scripts/forums/photo.js"));
            bundles.Add(new ScriptBundle("~/bundles/admin/structure").Include("~/Scripts/admin/views/adminstructure.js"));
            bundles.Add(new ScriptBundle("~/bundles/admin/users").Include("~/Scripts/admin/views/adminusers.js"));
            bundles.Add(new ScriptBundle("~/bundles/admin/user").Include("~/Scripts/admin/views/adminuserdetail.js"));

            bundles.Add(new LessBundle("~/content/less").Include("~/Content/*.less"));

            // CSS
            bundles.Add(new StyleBundle("~/content/css").Include(
                "~/content/bootstrap.css",
                "~/content/bootstrap-select.css",
                "~/content/jquery.mininotification.css",
                "~/scripts/dropzone/dropzone.css",
                "~/content/popup.css",
                "~/content/lightgallery.css",
                "~/content/lg-fb-comment-box.css",
                "~/content/PhotoSwipe/photoswipe.css",
                "~/content/PhotoSwipe/default-skin/default-skin.css",
                "~/content/site.css",
                "~/content/themes/white.css"));

            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            #if DEBUG
            BundleTable.EnableOptimizations = false;
            #else
            BundleTable.EnableOptimizations = true;
            #endif
        }
    }
}