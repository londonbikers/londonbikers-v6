using System.Web.Mvc;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsBrowseController : Controller
    {
        // GET: Forums/Browse
        public ActionResult Index()
        {
            return View("Browse");
        }
    }
}