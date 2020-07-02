using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace LBV6.Hubs
{
    public class ForumsHomepageHub : Hub
    {
        /// <summary>
        /// Registers that the user is currently viewing the forum homepage so that they can receive real-time updates to forum stats when new topics and replies are created.
        /// </summary>
        public Task ViewingForumsHomepage()
        {
            return Groups.Add(Context.ConnectionId, "ForumsHomepage");
        }
    }
}