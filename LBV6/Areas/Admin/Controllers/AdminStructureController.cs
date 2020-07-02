using System.Web.Mvc;

namespace LBV6.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminStructureController : Controller
    {
        // GET: Admin/AdminStructure
        public ActionResult Index()
        {
            return View();
        }
    }
}