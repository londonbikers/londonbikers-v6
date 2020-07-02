using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace LBV6.Hubs
{
    public class PopularTopicsHub : Hub
    {
        /// <summary>
        /// Registers that the user is currently viewing the popular topics view so that they can receive real-time updates when the popular topics list changes or new replies are created.
        /// </summary>
        public Task ViewingPopularTopics()
        {
            return Groups.Add(Context.ConnectionId, "PopularTopics");
        }
    }
}