using LBV6Library;
using LBV6Library.Models;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    /// <summary>
    /// The BroadcastController listens for updates to domain models and broadcasts their updates to any web clients listening
    /// so that their views can automatically update to reflect the changes.
    /// </summary>
    public class BroadcastController
    {
        #region accessors
        /// <summary>
        /// Enables the controller to send updates of topics (and replies) to clients connected to the web-app, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> TopicClients { get; set; }

        /// <summary>
        /// Enables the controller to send updates to topics (and replies) for a specific forum to clients connected to the web-app, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> ForumClients { get; set; }

        /// <summary>
        /// Enables the controller to send updates to topics (and replies) to clients viewing the forum homepage, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> ForumHomepageClients { get; set; }

        /// <summary>
        /// Enables the controller to send updates to topics (and replies) to clients viewing the latest topics view, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> LatestTopicsClients { get; set; }

        /// <summary>
        /// Enables the controller to send updates to topics (and replies) to clients viewing the popular topics view, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> PopularTopicsClients { get; set; }

        /// <summary>
        /// Enables the controller to send updates to users to clients viewing the admin users page, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> UsersClients { get; set; }
        #endregion

        #region constructors
        internal BroadcastController()
        {
        }
        #endregion

        #region dependency registration
        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterTopicClients(IHubConnectionContext<dynamic> clients)
        {
            if (TopicClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            TopicClients = clients;
        }

        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterForumClients(IHubConnectionContext<dynamic> clients)
        {
            if (ForumClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            ForumClients = clients;
        }

        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterForumHomepageClients(IHubConnectionContext<dynamic> clients)
        {
            if (ForumHomepageClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            ForumHomepageClients = clients;
        }

        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterLatestTopicsClients(IHubConnectionContext<dynamic> clients)
        {
            if (LatestTopicsClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            LatestTopicsClients = clients;
        }

        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterPopularTopicsClients(IHubConnectionContext<dynamic> clients)
        {
            if (PopularTopicsClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            PopularTopicsClients = clients;
        }

        /// <summary>
        /// For the controller to send updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterUsersClients(IHubConnectionContext<dynamic> clients)
        {
            if (UsersClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            UsersClients = clients;
        }
        #endregion

        #region posts
        /// <summary>
        /// Performs processing of the TopicCreated event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid impacting the UX.
        /// </summary>
        internal void ProcessTopicCreated(Post topic)
        {
            if (!topic.ForumId.HasValue)
                throw new ArgumentException("Topic has no forum id.");

            // broadcast to clients viewing the forum the reply topic resides in
            // as the topic headers contain user-specific information, we can't send out one model for all users
            // so instead we'll ask the user to make an API call to update the view if they're viewing the updated topic
            // not quite so ideal but better than nothing
            if (ForumClients != null)
                ForumClients.Group("Forum-" + topic.ForumId.Value).receiveNewTopic();

            if (ForumHomepageClients != null)
            {
                var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                var forumDto = Transformations.ConvertForumToForumDto(forum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(forumDto);
            }

            // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
            if (LatestTopicsClients != null)
                LatestTopicsClients.Group("LatestTopics").receiveNewTopic();
        }

        /// <summary>
        /// Performs processing of the TopicEdited event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid impacting the UX.
        /// </summary>
        internal async Task ProcessTopicEditedAsync(Post topic)
        {
            // validation
            if (topic.PostId.HasValue)
                throw new ArgumentException("Post is a reply not a topic!");

            if (TopicClients == null)
                return;

            // broadcast to clients viewing the forum the topic resides in
            // as the topic headers contain user-specific information, we can't send out one model for all users
            // so instead we'll ask the user to make an API call to update the view if they're viewing the updated topic
            // not quite so ideal but better than nothing
            if (topic.ForumId.HasValue)
            {
                if (ForumClients != null)
                    ForumClients.Group("Forum-" + topic.ForumId.Value).receiveUpdatedTopic(topic.Id);
               
                // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
                if (LatestTopicsClients != null)
                    LatestTopicsClients.Group("LatestTopics").receiveUpdatedTopic(topic.Id);
                    
                if (PopularTopicsClients != null)
                    PopularTopicsClients.Group("PopularTopics").receiveUpdatedTopic(topic.Id);
            }
        
            var topicDto = await Transformations.ConvertTopicToTopicDtoAsync(
                topic, 
                ForumServer.Instance.Categories.Categories, 
                ForumServer.Instance.Users.GetUserAsync, 
                ForumServer.Instance.Forums.GetForum);

            // broadcast to clients viewing the topic
            if (TopicClients != null)
                TopicClients.Group("Topic-" + topic.Id).receiveUpdatedTopic(topicDto);
        }

        /// <summary>
        /// Performs processing of the TopicMoved event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid impacting the UX.
        /// </summary>
        internal async Task ProcessTopicMovedAsync(Post topic, long oldForumId)
        {
            // validation
            if (topic.PostId.HasValue)
                throw new ArgumentException("Post is a reply not a topic!");

            // broadcast to clients viewing the old forum so we remove the topic
            if (ForumClients != null)
                ForumClients.Group("Forum-" + oldForumId).removeTopic(topic.Id);

            // broadcast to clients viewing the new forum so they refresh their view
            if (topic.ForumId.HasValue && ForumClients != null)
                ForumClients.Group("Forum-" + topic.ForumId.Value).receiveNewTopic();

            if (ForumHomepageClients != null && topic.ForumId.HasValue)
            {
                // update new forum
                var oldForum = ForumServer.Instance.Forums.GetForum(oldForumId);
                var oldForumDto = Transformations.ConvertForumToForumDto(oldForum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(oldForumDto);

                // update new forum
                var newForum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                var newForumDto = Transformations.ConvertForumToForumDto(newForum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(newForumDto);
            }

            // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
            if (LatestTopicsClients != null)
                LatestTopicsClients.Group("LatestTopics").receiveUpdatedTopic(topic.Id);
                
            if (PopularTopicsClients != null)
                PopularTopicsClients.Group("PopularTopics").receiveUpdatedTopic(topic.Id);

            var topicDto = await Transformations.ConvertTopicToTopicDtoAsync(
                topic,
                ForumServer.Instance.Categories.Categories,
                ForumServer.Instance.Users.GetUserAsync,
                ForumServer.Instance.Forums.GetForum);

            // broadcast to clients viewing the topic
            if (TopicClients != null)
                TopicClients.Group("Topic-" + topic.Id).receiveUpdatedTopic(topicDto);
        }

        /// <summary>
        /// Performs processing of the TopicRemoved event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid impacting the UX.
        /// </summary>
        internal void ProcessTopicRemoved(Post topic)
        {
            // validation
            if (topic.PostId.HasValue)
                throw new ArgumentException("Post is a reply not a topic!");

            if (ForumHomepageClients != null && topic.ForumId.HasValue)
            {
                var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                var forumDto = Transformations.ConvertForumToForumDto(forum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(forumDto);
            }

            if (topic.ForumId.HasValue && ForumClients != null)
                ForumClients.Group("Forum-" + topic.ForumId.Value).removeTopic(topic.Id);

            // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
            if (LatestTopicsClients != null)
                LatestTopicsClients.Group("LatestTopics").removeTopic(topic.Id);

            // broadcast to clients viewing the topic
            if (TopicClients != null)
                TopicClients.Group("Topic-" + topic.Id).removeTopic();
        }

        /// <summary>
        /// Performs processing of the ReplyCreated event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        internal async Task ProcessReplyCreatedAsync(Post reply)
        {
            // validation
            if (!reply.PostId.HasValue)
                throw new ArgumentException("Post is a topic not a reply!");

            if (TopicClients == null)
                return;

            var topic = await ForumServer.Instance.Posts.GetTopicAsync(reply.PostId.Value);
            var replyDto = await Transformations.ConvertReplyToReplyDtoAsync(reply, ForumServer.Instance.Users.GetUserAsync);

            if (ForumHomepageClients != null && topic.ForumId.HasValue)
            {
                var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                var forumDto = Transformations.ConvertForumToForumDto(forum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(forumDto);
            }

            // broadcast to clients viewing the forum the reply topic resides in
            // as the topic headers contain user-specific information, we can't send out one model for all users
            // so instead we'll ask the user to make an api call to update the view if they're viewing the updated topic
            // not quite so ideal but better than nothing
            if (topic.ForumId.HasValue && ForumClients != null)
                ForumClients.Group("Forum-" + topic.ForumId.Value).receiveUpdatedTopic(topic.Id);

            // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
            if (LatestTopicsClients != null)
                LatestTopicsClients.Group("LatestTopics").receiveUpdatedTopic(topic.Id);
                
            if (PopularTopicsClients != null)
                PopularTopicsClients.Group("PopularTopics").receiveUpdatedTopic(topic.Id);

            // broadcast to clients viewing the topic
            if (TopicClients != null)
            {
                var dto = new { Reply = replyDto, TotalReplies = topic.ReplyCountPublic };
                TopicClients.Group("Topic-" + topic.Id).receiveNewReply(dto);
                Logging.LogDebug(GetType().FullName, "Sent new reply to group: Topic-" + topic.Id);
            }
        }

        /// <summary>
        /// Performs processing of the ReplyEdited event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        internal async Task ProcessReplyEditedAsync(Post reply)
        {
            // validation
            if (!reply.PostId.HasValue)
                throw new ArgumentException("Post is a topic not a reply!");

            if (TopicClients == null)
                return;
 
            var replyDto = await Transformations.ConvertReplyToReplyDtoAsync(reply, ForumServer.Instance.Users.GetUserAsync);
                
            // broadcast to clients viewing the topic
            if (TopicClients != null)
                TopicClients.Group("Topic-" + reply.PostId.Value).receiveUpdatedReply(replyDto);
        }

        /// <summary>
        /// Performs processing of the ReplyRemoved event, pushing updates to web-clients as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        internal async Task ProcessReplyRemovedAsync(Post reply)
        {
            // validation
            if (!reply.PostId.HasValue)
                throw new ArgumentException("Post is a topic not a reply!");

            // broadcast to clients viewing the forum the reply topic resides in
            // as the topic headers contain user-specific information, we can't send out one model for all users
            // so instead we'll ask the user to make an API call to update the view if they're viewing the updated topic
            // not quite so ideal but better than nothing
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(reply.PostId.Value);

            if (ForumHomepageClients != null && topic.ForumId.HasValue)
            {
                var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                var forumDto = Transformations.ConvertForumToForumDto(forum, ForumServer.Instance.Categories.Categories);
                ForumHomepageClients.All.receiveUpdatedForum(forumDto);
            }

            if (topic.ForumId.HasValue && ForumClients != null)
                ForumClients.Group("Forum-" + topic.ForumId.Value).receiveUpdatedTopic(topic.Id);

            // we might have to consider scoping this tighter, i.e. by page, to avoid excessive comms to people viewing the latest topics page.
            if (LatestTopicsClients != null)
                LatestTopicsClients.Group("LatestTopics").receiveUpdatedTopic(topic.Id);
                
            if (PopularTopicsClients != null)
                PopularTopicsClients.Group("PopularTopics").receiveUpdatedTopic(topic.Id);

            // broadcast to clients viewing the topic
            if (TopicClients != null)
                TopicClients.Group("Topic-" + reply.PostId.Value).removeReply(reply.Id);
        }
        
        internal void ProcessPopularTopicsUpdate()
        {
            if (PopularTopicsClients != null)
                PopularTopicsClients.Group("PopularTopics").refresh();
        }
        #endregion

        #region users

        internal void ProcessUserCreated(string userId)
        {
            // broadcast to clients viewing the admin users page
            if (UsersClients != null)
                UsersClients.All.receiveRefresh();
        }

        internal void ProcessUserUpdated(string userId)
        {
            // broadcast to clients viewing the admin users page
            if (UsersClients != null)
                UsersClients.All.receiveUpdatedUser(userId);
        }

        internal void ProcessUserRemoved(string userId)
        {
            // broadcast to clients viewing the admin users page
            if (UsersClients != null)
                UsersClients.All.receiveRemovedUser(userId);
        }
        #endregion
    }
}
