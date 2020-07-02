using System.Web.Mvc;

namespace LBV6.Areas.Forums
{
    public class ForumsAreaRegistration : AreaRegistration
    {
        public override string AreaName => "Forums";

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("Categories", "forums/categories/{categoryId}/{name}", new { controller = "ForumsCategories", action = "Category", name = UrlParameter.Optional });
            context.MapRoute("Popular", "forums/popular", new { controller = "ForumsPopular", action = "Index" });
            context.MapRoute("Latest", "forums/browse", new { controller = "ForumsBrowse", action = "Index" });
            context.MapRoute("PostsConversion", "forums/posts-conversion", new { controller = "ForumsTopics", action = "ConvertGalleryUrl" });
            context.MapRoute("Forums", "forums/{forumId}/{name}", new { controller = "ForumsForums", action = "Forum", name = UrlParameter.Optional });
            context.MapRoute("Posts", "forums/posts/{topicId}/{encodedSubject}", new { controller = "ForumsTopics", action = "Topic", encodedSubject = UrlParameter.Optional });
            context.MapRoute("PostsPhoto", "forums/posts/{topicId}/photos/{photoId}", new { controller = "ForumsTopics", action = "TopicPhoto" });

            context.MapRoute(
                "Forums_default",
                "Forums/{controller}/{action}/{id}",
                new { controller = "ForumsHome", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}