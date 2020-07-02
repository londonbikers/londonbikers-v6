using System.Linq;
using System.Web.Mvc;
using LBV6ForumApp;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsCategoriesController : Controller
    {
        // GET: Forums/Categories/12/Category_Name
        //[OutputCache(Duration = 60, VaryByParam = "categoryId")]
        public ActionResult Category(long categoryId)
        {
            var category = ForumServer.Instance.Categories.Categories.SingleOrDefault(q => q.Id.Equals(categoryId));
            if (category == null)
                return Redirect("/forums");

            ViewBag.Title = category.Name;
            ViewBag.CategoryId = categoryId;
            return View();
        }
    }
}