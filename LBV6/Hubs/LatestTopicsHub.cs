using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace LBV6.Hubs
{
    public class LatestTopicsHub : Hub
    {
        /// <summary>
        /// Registers that the user is currently viewing the latest topics view so that they can receive real-time updates when there are new topics and replies
        /// </summary>
        public Task ViewingLatestTopics()
        {
            return Groups.Add(Context.ConnectionId, "LatestTopics");
        }
    }
}