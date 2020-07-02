using LBV6Library;
using System;
using System.Web.Mvc;

namespace LBV6.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
                return Redirect("/forums");

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            return View();
        }

        public ActionResult Rules()
        {
            ViewBag.Message = "Our rules";
            return View();
        }

        public ActionResult Change()
        {
            return View();
        }

        public ActionResult Error()
        {
            var exception = Server.GetLastError();
            if (exception == null)
                return View();

            Logging.LogPageError(GetType().FullName, Request.QueryString["aspxerrorpath"], exception);
            Helpers.Telemetry.TrackException(exception);
            return View();
        }

        public ActionResult Privacy()
        {
            return View();
        }

        public ActionResult CannotSignin()
        {
            return View();
        }

        public ActionResult NotFound()
        {
            return View();
        }

        public ActionResult ErrorTest()
        {
            throw new Exception("This is a test exception to ensure the error page and logging work.");
        }
    }
}