using LBV6ForumApp;
using LBV6Library;
using Newtonsoft.Json;
using StackExchange.Profiling;
using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LBV6.Controllers
{
    public class UsersController : Controller
    {
        //[OutputCache(Duration = 60, VaryByParam = "*")]
        public async Task<ActionResult> UserProfile(string username)
        {
            // we use a custom form of URL encoding for usernames as there's a lot of users with spaces in their names
            // and we don't want URLs to look like londonbikers.com/750%20man
            username = username.Replace("+", " ");
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(username);
            if (user == null)
                return new HttpNotFoundResult("No such user found.");

            // ensure extended profile information has been loaded
            if (!user.TopicsCount.HasValue)
            {
                var profiler = MiniProfiler.Current;
                using (profiler.Step("Get extended user information"))
                    await ForumServer.Instance.Users.GetUserExtendedInformationAsync(user);
            }

            var userDto = Transformations.ConvertUserToUserProfileDto(user);
            ViewBag.Payload = JsonConvert.SerializeObject(userDto);
            ViewBag.Title = user.UserName;
            ViewBag.Description = !string.IsNullOrEmpty(user.Biography) ? Utilities.GetContentSynopsis(user.Biography) : $"Profile for {user.UserName}.";
            ViewBag.User = user;

            #region opengraph
            // facebook open graph variables
            if (Request.Url != null) ViewBag.OgUrl = Request.Url.AbsoluteUri;
            ViewBag.OgTitle = user.UserName + "'s profile";
            ViewBag.OgDescription = ViewBag.Description;

            if (user.CoverPhotoId.HasValue)
            {
                ViewBag.OgImage = Helpers.GetUserCoverPhotoUrl(user, true);

                if (user.CoverPhotoWidth.HasValue && user.CoverPhotoHeight.HasValue)
                {
                    var factor = Convert.ToDouble(user.CoverPhotoWidth.Value) / Convert.ToDouble(user.CoverPhotoHeight.Value);
                    ViewBag.OgImageWidth = "1200";
                    ViewBag.OgImageHeight = Math.Round(1200D/factor).ToString(CultureInfo.CurrentCulture);
                }
            }
            #endregion

            return View();
        }
    }
}