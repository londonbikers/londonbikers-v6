using System.Web.Mvc;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsLatestController : Controller
    {
        // GET: forums/latest
        //[OutputCache(Duration = 60, VaryByParam = "*")]
        public ActionResult Index()
        {
            return View();
        }
    }
}