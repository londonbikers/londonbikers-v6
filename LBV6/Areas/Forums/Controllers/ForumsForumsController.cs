using System.Web.Mvc;
using LBV6ForumApp;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsForumsController : Controller
    {
        // GET: /forums/234/motorcycles
        public ActionResult Forum(long forumId)
        {
            var forum = ForumServer.Instance.Forums.GetForum(forumId);
            if (forum == null || !Helpers.CanUserAccessForum(forum))
                return Redirect("/forums");

            ViewBag.Title = forum.Name;
            ViewBag.ForumId = forumId;
            return View();
        }
    }
}