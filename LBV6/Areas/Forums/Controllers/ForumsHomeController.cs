using System.Web.Mvc;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsHomeController : Controller
    {
        // GET: Forums/
        public ActionResult Index()
        {
            return View("Latest");
        }
    }
}