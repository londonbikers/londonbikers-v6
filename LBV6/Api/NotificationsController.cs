using LBV6ForumApp;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Web.Http;

namespace LBV6.Api
{
    public class NotificationsApiController : ApiController
    {
        [HttpDelete]
        public async Task<IHttpActionResult> ClearAll()
        {
            await ForumServer.Instance.Notifications.ClearAllNotificationsAsync(User.Identity.GetUserId());
            return Ok();
        }
    }
}