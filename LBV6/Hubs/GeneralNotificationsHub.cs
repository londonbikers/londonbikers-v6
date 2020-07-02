using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace LBV6.Hubs
{
    public class GeneralNotificationsHub : Hub
    {
        public async Task<List<NotificationDto>> GetNotifications()
        {
            // todo: work out how to mitigate the db reads in here so they don't impact performance and scalability under high load
            // thinking of putting them in the cache on session start, which can be updated by the notification controller
            // and then removed when the session ends. will do this when we've moved to Azure Redis cache.

            var userId = HttpContext.Current.User.Identity.GetUserId();

            var notifications = (await ForumServer.Instance.Notifications.GetNotificationsAsync(userId)).Where(q => q.ScenarioType.HasValue && q.ScenarioType.Value != NotificationType.NewPrivateMessage).ToList();

            var notificationDtos = await Transformations.ConvertNotificationsToNotificationDtosAsync(
                notifications,
                ForumServer.Instance.Posts.GetTopicAsync,
                ForumServer.Instance.Forums.GetForum,
                ForumServer.Instance.Posts.GetLastReplySeenForTopicAsync,
                ForumServer.Instance.Users.GetUserAsync,
                userId);

            return notificationDtos;
        }
    }
}