using LBV6ForumApp;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(LBV6.Startup))]
namespace LBV6
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);

            var idProvider = new CustomSignalrUserIdProvider();
            GlobalHost.DependencyResolver.Register(typeof(IUserIdProvider), () => idProvider);
            app.MapSignalR();

            ForumServer.Instance.Notifications.RegisterGeneralNotificationClients(GlobalHost.ConnectionManager.GetHubContext("GeneralNotificationsHub").Clients);
            ForumServer.Instance.Notifications.RegisterIntercomNotificationClients(GlobalHost.ConnectionManager.GetHubContext("IntercomNotificationsHub").Clients);
            ForumServer.Instance.Messages.RegisterClients(GlobalHost.ConnectionManager.GetHubContext("IntercomHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterTopicClients(GlobalHost.ConnectionManager.GetHubContext("TopicsHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterForumClients(GlobalHost.ConnectionManager.GetHubContext("ForumsHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterForumHomepageClients(GlobalHost.ConnectionManager.GetHubContext("ForumsHomepageHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterLatestTopicsClients(GlobalHost.ConnectionManager.GetHubContext("LatestTopicsHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterPopularTopicsClients(GlobalHost.ConnectionManager.GetHubContext("PopularTopicsHub").Clients);
            ForumServer.Instance.Broadcasts.RegisterUsersClients(GlobalHost.ConnectionManager.GetHubContext("UsersHub").Clients);
        }
    }
}