using LBV6Library;
using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace LBV6.Hubs
{
    public class TopicsHub : Hub
    {
        /// <summary>
        /// Registers that the user is currently viewing a particular topic so that they can receive real-time updates to the topic.
        /// </summary>
        public Task ViewingTopic(long topicId)
        {
            Logging.LogDebug(GetType().FullName, $"ViewingTopic: ConnectionId: {Context.ConnectionId}, Group: Topic-{topicId}");
            return Groups.Add(Context.ConnectionId, "Topic-" + topicId);
        }
    }
}