using LBV6ForumApp;
using LBV6Library;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LBV6.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class AdminUsersController : Controller
    {
        // GET: admin/users
        public ActionResult Index()
        {
            return View();
        }

        // GET: admin/users/detail/07e7d48d-0807-4177-a25d-30ab94947d74
        public async Task<ActionResult> Detail(string id)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(id);
            if (user == null)
                return HttpNotFound("No such user found.");

            if (!user.TopicsCount.HasValue)
                await ForumServer.Instance.Users.GetUserExtendedInformationAsync(user);

            var dto = Transformations.ConvertUserToUserProfileExtendedDto(user);
            ViewBag.Payload = JsonConvert.SerializeObject(dto);
            ViewBag.Title = user.UserName + " - Admin";
            return View();
        }
    }
}