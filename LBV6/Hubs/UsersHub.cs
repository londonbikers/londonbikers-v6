using Microsoft.AspNet.SignalR;
using System.Threading.Tasks;

namespace LBV6.Hubs
{
    [Authorize(Roles = "Administrator")]
    public class UsersHub : Hub
    {
        ///// <summary>
        ///// Registers that the user is currently viewing the latest topics view so that they can receive real-time updates when there are new topics and replies
        ///// </summary>
        //public Task ViewingLatestUsers()
        //{
        //    return Groups.Add(Context.ConnectionId, "LatestUsers");
        //}

        //public Task NotViewingLatestUsers()
        //{
        //    return Groups.Remove(Context.ConnectionId, "LatestUsers");
        //}
    }
}