using System.Web.Mvc;

namespace LBV6.Controllers
{
    [Authorize]
    public class IntercomController : Controller
    {
        // GET: intercom
        public ActionResult Index()
        {
            return View();
        }
    }
}