using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace LBV6.Hubs
{
    public class ForumsHub : Hub
    {
        /// <summary>
        /// Registers that the user is currently viewing a particular forum so that they can receive real-time updates as new forum topics or replies are created.
        /// </summary>
        public Task ViewingForum(long forumId)
        {
            return Groups.Add(Context.ConnectionId, "Forum-" + forumId);
        }

        /// <summary>
        /// Registers that the user is not currently viewing a particular forum so that they stop receiving real-time updates as new forum topics or replies are created.
        /// </summary>
        public Task NotViewingForum(long forumId)
        {
            return Groups.Remove(Context.ConnectionId, "Forum-" + forumId);
        }
    }
}