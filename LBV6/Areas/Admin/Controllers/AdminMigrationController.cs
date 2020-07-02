using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity.Owin;

namespace LBV6.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminMigrationController : Controller
    {
        #region members
        private ApplicationUserManager _userManager;
        #endregion

        #region accessors
        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        #endregion

        #region constructors
        public AdminMigrationController()
        {
        }

        public AdminMigrationController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
        }
        #endregion

        // GET: Admin/AdminMigration
        public ActionResult Index()
        {
            return View();
        }
    }
}
