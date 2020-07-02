using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System.Web;

namespace LBV6
{
    /// <summary>
    /// SignalR will use the identity name, not the identity id by default. Let's change that.
    /// </summary>
    public class CustomSignalrUserIdProvider : IUserIdProvider
    {
        public string GetUserId(IRequest request)
        {
            return HttpContext.Current.User.Identity.GetUserId();
        }
    }
}