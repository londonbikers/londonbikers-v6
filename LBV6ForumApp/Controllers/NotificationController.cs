using LBV6Library;
using LBV6Library.Models;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    public class NotificationController
    {
        #region members
        /// <summary>
        /// After a user receives their first notification they won't receive another until a threshold number of occurrences is met.
        /// </summary>
        private const int NewForumTopicUpdateThreshold = 5;

        /// <summary>
        /// After a user receives their first notification they won't receive another until a threshold number of occurrences is met.
        /// </summary>
        private const int NewTopicReplyUpdateThreshold = 5;

        /// <summary>
        /// After a user receives their first notification they won't receive another until a threshold number of occurrences is met.
        /// </summary>
        private const int NewPrivateMessageUpdateThreshold = 5;
        #endregion

        #region accessors
        /// <summary>
        /// Enables the controller to send Intercom notifications to clients connected to the web-app in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> IntercomNotificationClients { get; set; }
        /// <summary>
        /// Enables the controller to send general notifications to clients connected to the web-app in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> GeneralNotificationClients { get; set; }
        #endregion

        #region constructors
        internal NotificationController(ForumServer forumServer)
        {
            // we use a reference to the forum server instead of accessing the normal ForumServer.Instance property as the forum server is not fully initialised 
            // when this code is called and using this method would cause a stack-overflow as another forum server 
            // instance is instantiated despite the fact it follows a singleton pattern.

            forumServer.Messages.PrivateMessageCreated += ProcessMessageCreatedHandler;
            forumServer.Messages.UserUnreadMessageCountChanged += ProcessUnreadMessageCountChangedHandler;
        }
        #endregion

        #region dependency registration
        /// <summary>
        /// For the controller to send intercom updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at start-up.
        /// </summary>
        public void RegisterIntercomNotificationClients(IHubConnectionContext<dynamic> clients)
        {
            if (IntercomNotificationClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            IntercomNotificationClients = clients;
        }

        /// <summary>
        /// For the controller to send notifications to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at start-up.
        /// </summary>
        public void RegisterGeneralNotificationClients(IHubConnectionContext<dynamic> clients)
        {
            if (GeneralNotificationClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            GeneralNotificationClients = clients;
        }
        #endregion

        #region manage notification subscriptions
        public async Task<bool> IsUserSubscribedToTopicAsync(string userId, long topicId)
        {
            using (var db = new ForumContext())
                return await db.NotificationSubscriptions.AnyAsync(q => q.UserId.Equals(userId) && q.SubjectId.Equals(topicId));
        }

        public async Task<bool> IsUserSubscribedToForumAsync(string userId, long forumId)
        {
            using (var db = new ForumContext())
                return await db.NotificationSubscriptions.AnyAsync(q => q.UserId.Equals(userId) && q.SubjectId.Equals(forumId));
        }

        public async Task SubscribeToTopicAsync(User user, Post topic)
        {
            // validate
            if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User has not been persisted.");
            if (topic.Id < 1) throw new ArgumentException("Topic isn't persisted.");
            if (topic.PostId.HasValue) throw new ArgumentException("Post is a reply, not a topic.");

            using (var db = new ForumContext())
            {
                // make sure the user isn't already subscribed, we don't want to create a duplicate
                var dbSubscription = await db.NotificationSubscriptions.SingleOrDefaultAsync(q => q.UserId.Equals(user.Id) && q.SubjectId.Equals(topic.Id));
                if (dbSubscription != null)
                    return;

                var subscription = new NotificationSubscription
                {
                    UserId = user.Id,
                    SubjectId = topic.Id,
                    Type = NotificationType.NewTopicReply
                };

                db.NotificationSubscriptions.Add(subscription);
                await db.SaveChangesAsync();
            }
        }

        public async Task UnSubscribeFromTopicAsync(User user, Post topic)
        {
            // validate
            if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User has not been persisted.");
            if (topic.Id < 1) throw new ArgumentException("Topic isn't persisted.");
            if (topic.PostId.HasValue) throw new ArgumentException("Post is a reply, not a topic.");

            using (var db = new ForumContext())
            {
                // make sure the user isn't already subscribed, we don't want to create a duplicate
                var dbSubscription = await db.NotificationSubscriptions.SingleOrDefaultAsync(q => q.UserId.Equals(user.Id) && q.SubjectId.Equals(topic.Id) && q.Type == NotificationType.NewTopic);
                if (dbSubscription == null)
                    return;

                db.NotificationSubscriptions.Remove(dbSubscription);
                await db.SaveChangesAsync();
            }
        }

        public async Task SubscribeToForumAsync(User user, Forum forum)
        {
            // validate
            if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User has not been persisted.");
            if (forum.Id < 1) throw new ArgumentException("Forum isn't persisted.");

            using (var db = new ForumContext())
            {
                // make sure the user isn't already subscribed, we don't want to create a duplicate
                var dbSubscription = await db.NotificationSubscriptions.SingleOrDefaultAsync(q => q.UserId.Equals(user.Id) && q.SubjectId.Equals(forum.Id) && q.Type == NotificationType.NewTopic);
                if (dbSubscription != null)
                    return;

                var subscription = new NotificationSubscription
                {
                    UserId = user.Id,
                    SubjectId = forum.Id,
                    Type = NotificationType.NewTopic
                };

                db.NotificationSubscriptions.Add(subscription);
                await db.SaveChangesAsync();
            }
        }

        public async Task UnSubscribeFromForumAsync(User user, Forum forum)
        {
            // validate
            if (string.IsNullOrEmpty(user.Id)) throw new ArgumentException("User has not been persisted.");
            if (forum.Id < 1) throw new ArgumentException("Forum isn't persisted.");

            using (var db = new ForumContext())
            {
                // make sure the user isn't already subscribed, we don't want to create a duplicate
                var dbSubscription = await db.NotificationSubscriptions.SingleOrDefaultAsync(q => q.UserId.Equals(user.Id) && q.SubjectId.Equals(forum.Id));
                if (dbSubscription == null)
                    return;

                db.NotificationSubscriptions.Remove(dbSubscription);
                await db.SaveChangesAsync();
            }
        }
        #endregion

        /// <summary>
        /// Gets the list of notifications for a user, ordered by last updated first.
        /// </summary>
        public async Task<List<Notification>> GetNotificationsAsync(string userId)
        {
            // todo: retrieve from the cache somehow
            using (var db = new ForumContext())
                return await db.Notifications.Where(q => q.UserId.Equals(userId)).OrderByDescending(q => q.Updated).ToListAsync();
        }

        /// <summary>
        /// Deletes all of a users notifications in one go.
        /// </summary>
        public async Task ClearAllNotificationsAsync(string userId)
        {
            using (var db = new ForumContext())
            {
                db.Notifications.RemoveRange(db.Notifications.Where(q => q.UserId.Equals(userId)));
                await db.SaveChangesAsync();
            }
        }

        #region process notification creations
        /// <summary>
        /// Performs processing of the TopicCreated event, creating notifications as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        internal async Task ProcessTopicCreatedAsync(Post topic)
        {
            // look for any reply parent topic notification subscriptions
            if (!topic.ForumId.HasValue)
                throw new ArgumentException("Topic has no forum id.");

            using (var db = new ForumContext())
            {
                // do we have any subscriptions from other people for the forum that the topic resides in?
                foreach (var dbSubscription in await db.NotificationSubscriptions.Where(q => q.SubjectId.Equals(topic.ForumId.Value) && q.UserId != topic.UserId && q.Type == NotificationType.NewTopic).ToListAsync())
                {
                    var receivingUser = await ForumServer.Instance.Users.GetUserAsync(dbSubscription.UserId);

                    // new topic forum subscriptions result in new notifications being created (one for each topic) until we reach a threshold where we
                    // need to stop notifying the user, to avoid their notifications list for becoming unusable.
                    if (await db.Notifications.CountAsync(q => q.NotificationSubscriptionId.HasValue && q.NotificationSubscriptionId.Value.Equals(dbSubscription.Id)) <= NewForumTopicUpdateThreshold)
                    {
                        // under notification threshold; create notification that references content
                        var dbNotification = new Notification
                        {
                            ScenarioType = NotificationType.NewTopic,
                            NotificationSubscriptionId = dbSubscription.Id,
                            ContentId = topic.Id,
                            UserId = receivingUser.Id
                        };
                        db.Notifications.Add(dbNotification);

                        // send out any email notification as necessary
                        await ProcessNewTopicEmailNotificationAsync(dbSubscription, topic, receivingUser);
                    }
                    else
                    {
                        // over notification threshold; create notification that doesn't relate to content, but just the subscription.
                        // under notification threshold; create notification that references content
                        var dbNotification = new Notification { NotificationSubscriptionId = dbSubscription.Id };
                        db.Notifications.Add(dbNotification);
                    }

                    // update the user's notifications count
                    receivingUser.NonMessageNotificationsCount++;
                    await ForumServer.Instance.Users.UpdateUserAsync(receivingUser);

                    // todo: broadcast to client
                }

                // save changes to any subscriptions or notifications
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Performs processing of the ReplyCreated event, creating notifications as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        internal async Task ProcessReplyCreatedAsync(Post reply)
        {
            // validation
            if (!reply.PostId.HasValue)
                throw new ArgumentException("Post is a topic not a reply!");

            var topic = await ForumServer.Instance.Posts.GetTopicAsync(reply.PostId.Value);
            using (var db = new ForumContext())
            {
                #region implicit subscribers (thread members)
                // NOTIFICATION PATH 1...
                // get a list of the distinct users in the thread (excluding including the reply author) and issue notifications
                // todo: this needs to use preferences and have mute controls

                var topicUsers = new List<string> { topic.UserId };
                topicUsers.AddRange(topic.Replies.Select(q => q.UserId));
                topicUsers = topicUsers.Distinct().ToList();
                topicUsers.RemoveAll(q => q.Equals(reply.UserId));

                foreach (var userId in topicUsers)
                {
                    // todo: add in support for checking if there are any topic mutes (which don't exist yet)

                    var user = await ForumServer.Instance.Users.GetUserAsync(userId);
                    var dbNotification = await db.Notifications.SingleOrDefaultAsync(q => q.ContentId.HasValue && q.ContentId.Value.Equals(topic.Id) && q.UserId.Equals(userId) && q.ScenarioType.HasValue && q.ScenarioType.Value == NotificationType.NewTopicReply);
                    if (dbNotification != null)
                    {
                        // don't create a new notification, just increase the occurrence count
                        dbNotification.Occurances++;

                        // is the occurrence count at a threshold where we should send an email update
                        if (dbNotification.Occurances == NewTopicReplyUpdateThreshold)
                            await ProcessNewReplyBatchUpdateEmailNotificationAsync(dbNotification, topic, reply, user);
                    }
                    else
                    {
                        // new notification relating directly to content required
                        dbNotification = new Notification
                        {
                            ContentId = topic.Id,
                            ScenarioType = NotificationType.NewTopicReply,
                            UserId = userId
                        };
                        db.Notifications.Add(dbNotification);

                        // update the user's notifications count
                        user.NonMessageNotificationsCount++;
                        await ForumServer.Instance.Users.UpdateUserAsync(user);

                        // send out any email notification as necessary
                        await ProcessNewReplyEmailNotificationAsync(topic, reply, user);
                    }

                    // todo: broadcast to client
                }
                #endregion

                #region explicit subscribers
                // NOTIFICATION PATH 2
                // see if anyone is explicitly subscribed to the reply topic
                foreach (var dbSubscription in await db.NotificationSubscriptions.Where(q => q.SubjectId.Equals(topic.Id) && q.UserId != reply.UserId && q.Type == NotificationType.NewTopicReply).ToListAsync())
                {
                    if (topicUsers.Any(q => q.Equals(dbSubscription.UserId)))
                    {
                        Logging.LogDebug(GetType().FullName, $"User {dbSubscription.UserId} has already been processed as part of the thread membership.");
                        continue;
                    }

                    // is there a notification already?
                    // there can only be one notification for the NewTopicReply scenario
                    var user = await ForumServer.Instance.Users.GetUserAsync(dbSubscription.UserId);
                    var dbNotification = await db.Notifications.SingleOrDefaultAsync(q => q.NotificationSubscriptionId.HasValue && q.NotificationSubscriptionId.Value.Equals(dbSubscription.Id));
                    if (dbNotification != null)
                    {
                        // don't create a new notification, just increase the occurrence count
                        dbNotification.Occurances++;

                        // is the occurrence count at a threshold where we should send an email update
                        if (dbNotification.Occurances == NewTopicReplyUpdateThreshold)
                            await ProcessNewReplyBatchUpdateEmailNotificationAsync(dbNotification, topic, reply, user);
                    }
                    else
                    {
                        // new notification needed
                        dbNotification = new Notification
                        {
                            ContentId = topic.Id,
                            NotificationSubscriptionId = dbSubscription.Id,
                            ScenarioType = NotificationType.NewTopicReply,
                            UserId = user.Id
                        };

                        db.Notifications.Add(dbNotification);
                        await ProcessNewReplyEmailNotificationAsync(topic, reply, user);
                    }

                    // todo: broadcast to client
                }
                #endregion

                await db.SaveChangesAsync();
            }
        }

        internal async Task ProcessPhotoCommentCreatedAsync(Guid photoId, long photoCommentId, Post topic, Post reply = null)
        {
            var photo = Utilities.FindPhoto(topic, photoId);
            if (photo == null && reply == null)
                throw new ArgumentException("Couldn't find photo in the topic. Did you forget to supply the reply?");
            if (photo == null)
                photo = Utilities.FindPhoto(reply, photoId);

            var comment = Utilities.FindPhotoComment(photo.Comments, photoCommentId);
            if (comment == null)
                throw new ArgumentException($"Couldn't find comment with id {photoCommentId} in photo comments list.");

            using (var db = new ForumContext())
            {
                #region implicit subscribers (photo commentators and photo post author)
                // get a list of the distinct users in the comments list (excluding including the photo author) and issue notifications
                // todo: this needs to use preferences and have mute controls

                var users = new List<string>();
                users.AddRange(photo.Comments.Select(q => q.UserId));

                // make sure we're including the photo owner, even if they haven't left a comment
                users.Add(photo.UserId);

                users = users.Distinct().ToList();

                // don't send a notification to the person leaving the comment
                users.RemoveAll(q => q.Equals(comment.UserId));

                foreach (var userId in users)
                {
                    // todo: add in support for checking if there are any photo notification mutes (which don't exist yet)

                    var user = await ForumServer.Instance.Users.GetUserAsync(userId);
                    var dbNotification = await db.Notifications.SingleOrDefaultAsync(q => q.ContentGuid.HasValue && q.ContentGuid.Value.Equals(photoId) && q.UserId.Equals(userId) && q.ScenarioType.HasValue && q.ScenarioType.Value == NotificationType.NewTopicReply);
                    if (dbNotification != null)
                    {
                        // don't create a new notification, just increase the occurrence count
                        dbNotification.Occurances++;

                        // is the occurrence count at a threshold where we should send a follow-up email
                        if (dbNotification.Occurances == NewTopicReplyUpdateThreshold)
                            await ProcessNewPhotoCommentBatchUpdateEmailNotificationAsync(dbNotification, comment, photo, user, topic, reply);
                    }
                    else
                    {
                        // new notification relating directly to content required
                        dbNotification = new Notification
                        {
                            ContentGuid = photo.Id,
                            ContentParentId = topic.Id,
                            CommentId = photoCommentId,
                            ScenarioType = NotificationType.NewPhotoComment,
                            UserId = userId
                        };

                        if (reply != null)
                            dbNotification.ContentId = reply.Id;

                        db.Notifications.Add(dbNotification);

                        // update the user's notifications count
                        user.NonMessageNotificationsCount++;
                        await ForumServer.Instance.Users.UpdateUserAsync(user);

                        // send out any email notification as necessary
                        await ProcessNewPhotoCommentEmailNotificationAsync(comment, photo, user, topic, reply);
                    }

                    // todo: broadcast to client
                }
                #endregion

                await db.SaveChangesAsync();
            }
        }

        // todo: handle post moderation notifications

        /// <summary>
        /// Responsible for initiating handling of the MessageCreated event.
        /// </summary>
        private static void ProcessMessageCreatedHandler(PrivateMessage message)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessMessageCreatedAsync(message));
        }

        /// <summary>
        /// Performs processing of the MessageCreated event, creating notifications as necessary.
        /// Run this on a background thread to avoid slowing the UX.
        /// </summary>
        private static async Task ProcessMessageCreatedAsync(PrivateMessage message)
        {
            try
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Received an MessageCreated event notification for: " + message);
                var header = await ForumServer.Instance.Messages.GetHeaderAsync(message.PrivateMessageHeaderId);
                var author = await ForumServer.Instance.Users.GetUserAsync(message.UserId);

                using (var db = new ForumContext())
                {
                    foreach (var headerUser in header.Users.Where(q => q.UserId != message.UserId))
                    {
                        var recipient = await ForumServer.Instance.Users.GetUserAsync(headerUser.UserId);

                        // create or update the notification object
                        var dbNotification = await db.Notifications.SingleOrDefaultAsync(q =>
                            q.ContentId.HasValue &&
                            q.ContentId.Value.Equals(header.Id) &&
                            q.UserId.Equals(recipient.Id) &&
                            q.ScenarioType.HasValue &&
                            q.ScenarioType.Value == NotificationType.NewPrivateMessage);

                        if (dbNotification != null)
                        {
                            // existing notification, update how many occurrences we have for the header
                            dbNotification.Occurances++;
                            await db.SaveChangesAsync();

                            if (dbNotification.Occurances == NewPrivateMessageUpdateThreshold)
                                await ProcessNewMessageEmailNotificationAsync(message, author, recipient, EmailTemplate.NewPrivateMessageWithRollups);
                        }
                        else
                        {
                            // no notification (either new header or all previous messages in header read)
                            dbNotification = new Notification
                            {
                                ContentId = header.Id,
                                ScenarioType = NotificationType.NewPrivateMessage,
                                UserId = recipient.Id
                            };
                            db.Notifications.Add(dbNotification);
                            await db.SaveChangesAsync();

                            // send out any email notification as necessary
                            await ProcessNewMessageEmailNotificationAsync(message, author, recipient, EmailTemplate.NewPrivateMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // catching and logging here as we're on a separate thread
                Logging.LogError(typeof(NotificationController).FullName, ex);
            }
        }

        /// <summary>
        /// Responsible for initiating handling of the UnreadMessageCountChanged event.
        /// </summary>
        private static void ProcessUnreadMessageCountChangedHandler(User user)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(ct => ProcessUnreadMessageCountChanged(user));
        }

        private static void ProcessUnreadMessageCountChanged(User user)
        {
            // need to check if this exists as it won't for unit tests
            if (IntercomNotificationClients != null)
                IntercomNotificationClients.User(user.Id).updateUnreadMessageCount(user.UnreadMessagesCount);
        }
        #endregion

        #region process notification invalidations
        // todo: revise - for scalability?
        public async Task InvalidateSingleMessageNotificationAsync(string userId, long privateMessageHeaderId)
        {
            using (var db = new ForumContext())
            {
                // do we have a notification to invalidate?
                var dbNotification = await db.Notifications.SingleOrDefaultAsync(q =>
                            q.ContentId.HasValue &&
                            q.ContentId == privateMessageHeaderId &&
                            q.UserId == userId &&
                            q.ScenarioType.HasValue &&
                            q.ScenarioType.Value == NotificationType.NewPrivateMessage);

                if (dbNotification == null)
                    return;

                if (dbNotification.Occurances > 1)
                {
                    // multiple occurances, just decrease tne number
                    dbNotification.Occurances--;
                }
                else
                {
                    // only one occurance so delete the notification
                    db.Notifications.Remove(dbNotification);
                }

                await db.SaveChangesAsync();
            }
        }

        // todo: revise - for scalability?
        public async Task InvalidateAllMessageHeaderNotificationsAsync(string userId, long privateMessageHeaderId)
        {
            using (var db = new ForumContext())
            {
                // do we have a notification to invalidate?
                var dbNotification = await db.Notifications.SingleOrDefaultAsync(q =>
                            q.ContentId.HasValue &&
                            q.ContentId == privateMessageHeaderId &&
                            q.UserId == userId &&
                            q.ScenarioType.HasValue &&
                            q.ScenarioType.Value == NotificationType.NewPrivateMessage);

                if (dbNotification != null)
                {
                    db.Notifications.Remove(dbNotification);
                    await db.SaveChangesAsync();
                }
            }
        }

        // todo: revise - for scalability?
        public async Task InvalidateTopicViewNotificationAsync(string userId, long topicId)
        {
            using (var db = new ForumContext())
            {
                // do we have a notification to invalidate?
                var dbNotification = await db.Notifications.SingleOrDefaultAsync(q =>
                            q.ContentId.HasValue &&
                            q.ContentId == topicId &&
                            q.UserId == userId);

                if (dbNotification == null)
                    return;

                db.Notifications.Remove(dbNotification);
                await db.SaveChangesAsync();
            }
        }

        public async Task InvalidatePhotoViewNotificationAsync(string userId, Guid photoId)
        {
            using (var db = new ForumContext())
            {
                // do we have a notification to invalidate?
                var dbNotification = await db.Notifications.SingleOrDefaultAsync(q =>
                    q.ContentGuid.HasValue &&
                    q.ContentGuid == photoId &&
                    q.UserId == userId);

                if (dbNotification == null)
                    return;

                db.Notifications.Remove(dbNotification);
                await db.SaveChangesAsync();
            }
        }
        #endregion

        #region process email notifications
        private static async Task ProcessNewTopicEmailNotificationAsync(NotificationSubscription notificationSubscription, Post topic, User receivingUser)
        {
            if (!receivingUser.NewTopicNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new topic email notifications.");
                return;
            }

            if (!topic.ForumId.HasValue)
                throw new ArgumentException("Post is not a topic.");

            var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
            var category = ForumServer.Instance.Categories.Categories.Single(q => q.Id.Equals(forum.CategoryId));
            var subscriber = await ForumServer.Instance.Users.GetUserAsync(notificationSubscription.UserId);
            var poster = await ForumServer.Instance.Users.GetUserAsync(topic.UserId);
            var synopsis = Utilities.GetContentSynopsis(topic.Content);

            var emailSubject = "New topic: " + topic.Subject;
            List<object> emailParams;
            EmailTemplate emailTemplate;

            // choose the template
            switch (topic.Photos.Count)
            {
                case 0:
                    // no photos
                    emailTemplate = EmailTemplate.NewTopic;
                    emailParams = new List<object>
                    {
                        subscriber.UserName,
                        category.Name,
                        forum.Name,
                        topic.Subject,
                        poster.UserName,
                        synopsis,
                        Urls.GetTopicUrl(topic, true)
                    };
                    break;
                case 1:
                    // single photo
                    if (string.IsNullOrEmpty(topic.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicWithPhotoWithoutContent;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            category.Name,
                            forum.Name,
                            topic.Subject,
                            poster.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(topic.Photos[0]),
                            Urls.GetTopicUrl(topic, true)
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicWithPhoto;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            category.Name,
                            forum.Name,
                            topic.Subject,
                            poster.UserName,
                            synopsis,
                            Urls.GetTopicUrl(topic, true),
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(topic.Photos[0])
                        };
                    }
                    break;
                default:
                    if (string.IsNullOrEmpty(topic.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicWithPhotosWithoutContent;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            category.Name,
                            forum.Name,
                            topic.Subject,
                            poster.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(topic.Photos[0]),
                            topic.Photos.Count - 1,
                            Urls.GetTopicUrl(topic, true),
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicWithPhotos;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            category.Name,
                            forum.Name,
                            topic.Subject,
                            poster.UserName,
                            synopsis,
                            Urls.GetTopicUrl(topic, true),
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(topic.Photos[0]),
                            topic.Photos.Count - 1
                        };
                    }
                    break;
            }

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(emailTemplate, subscriber.Email, emailParams.ToArray(), emailSubject);
        }

        private static async Task ProcessNewReplyEmailNotificationAsync(Post topic, Post reply, User receivingUser)
        {
            if (!receivingUser.NewReplyNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new reply email notifications.");
                return;
            }

            // action it
            var recipient = await ForumServer.Instance.Users.GetUserAsync(receivingUser.Id);
            var replier = await ForumServer.Instance.Users.GetUserAsync(reply.UserId);
            var synopsis = Utilities.GetContentSynopsis(reply.Content);

            // send an email
            var emailSubject = "New reply to: " + topic.Subject;
            List<object> emailParams;
            EmailTemplate emailTemplate;
            var lastReplyIdSeen = await ForumServer.Instance.Posts.GetLastReplySeenForTopicAsync(topic, recipient);
            var topicUrl = Urls.GetTopicUrl(topic, true, lastReplyIdSeen);

            switch (reply.Photos.Count)
            {
                case 0:
                    // no photos
                    emailTemplate = EmailTemplate.NewTopicReply;
                    emailParams = new List<object>
                    {
                        recipient.UserName,
                        topic.Subject,
                        replier.UserName,
                        synopsis,
                        topicUrl
                    };
                    break;
                case 1:
                    // single photo
                    if (string.IsNullOrEmpty(reply.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotoWithoutContent;
                        emailParams = new List<object>
                        {
                            recipient.UserName,
                            topic.Subject,
                            replier.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            topicUrl
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhoto;
                        emailParams = new List<object>
                        {
                            recipient.UserName,
                            topic.Subject,
                            replier.UserName,
                            synopsis,
                            topicUrl,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0])
                        };
                    }
                    break;
                default:
                    // multiple photos
                    if (string.IsNullOrEmpty(reply.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotosWithoutContent;
                        emailParams = new List<object>
                        {
                            recipient.UserName,
                            topic.Subject,
                            replier.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            reply.Photos.Count - 1,
                            topicUrl
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotos;
                        emailParams = new List<object>
                        {
                            recipient.UserName,
                            topic.Subject,
                            replier.UserName,
                            synopsis,
                            topicUrl,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            reply.Photos.Count - 1
                        };
                    }
                    break;
            }

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(emailTemplate, receivingUser.Email, emailParams.ToArray(), emailSubject);
        }

        private static async Task ProcessNewReplyBatchUpdateEmailNotificationAsync(Notification notification, Post topic, Post reply, User receivingUser)
        {
            if (!receivingUser.NewReplyNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new reply email notifications.");
                return;
            }

            // action it
            var subscriber = await ForumServer.Instance.Users.GetUserAsync(receivingUser.Id);
            var replier = await ForumServer.Instance.Users.GetUserAsync(reply.UserId);
            var synopsis = Utilities.GetContentSynopsis(reply.Content);

            // send an email
            var emailSubject = "New reply to: " + topic.Subject;
            List<object> emailParams;
            EmailTemplate emailTemplate;
            var lastReplyIdSeen = await ForumServer.Instance.Posts.GetLastReplySeenForTopicAsync(topic, subscriber);
            var topicUrl = Urls.GetTopicUrl(topic, true, lastReplyIdSeen);

            switch (reply.Photos.Count)
            {
                case 0:
                    // no photos
                    emailTemplate = EmailTemplate.NewTopicReplyWithRollups;
                    emailParams = new List<object>
                    {
                        subscriber.UserName,
                        notification.Occurances.ToString(),
                        topic.Subject,
                        replier.UserName,
                        synopsis,
                        topicUrl
                    };
                    break;
                case 1:
                    // single photo
                    if (string.IsNullOrEmpty(reply.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotoWithoutContentWithRollups;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            notification.Occurances.ToString(),
                            topic.Subject,
                            replier.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            topicUrl
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotoWithRollups;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            notification.Occurances.ToString(),
                            topic.Subject,
                            replier.UserName,
                            synopsis,
                            topicUrl,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0])
                        };
                    }
                    break;
                default:
                    if (string.IsNullOrEmpty(reply.Content))
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotosWithoutContentWithRollups;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            notification.Occurances.ToString(),
                            topic.Subject,
                            replier.UserName,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            reply.Photos.Count - 1,
                            topicUrl
                        };
                    }
                    else
                    {
                        emailTemplate = EmailTemplate.NewTopicReplyWithPhotosWithRollups;
                        emailParams = new List<object>
                        {
                            subscriber.UserName,
                            notification.Occurances.ToString(),
                            topic.Subject,
                            replier.UserName,
                            synopsis,
                            topicUrl,
                            ConfigurationManager.AppSettings["LB.Url"],
                            Utilities.GetFileStoreIdForPhoto(reply.Photos[0]),
                            reply.Photos.Count - 1
                        };
                    }
                    break;
            }

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(emailTemplate, subscriber.Email, emailParams.ToArray(), emailSubject);
        }

        private static async Task ProcessNewMessageEmailNotificationAsync(PrivateMessage privateMessage, User authorUser, User receivingUser, EmailTemplate emailTemplate)
        {
            // users can control whether or not to receive message notifications..
            if (!receivingUser.NewMessageNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new private message email notifications to be sent.");
                return;
            }

            var synopsis = Utilities.GetContentSynopsis(privateMessage.Content);
            var emailSubject = $"New private message from {authorUser.UserName} - {privateMessage.Created.Ticks}";
            var emailParams = new List<object>
            {
                receivingUser.UserName,
                authorUser.UserName,
                synopsis,
                Urls.GetPrivateMessageAbsoluteUrl(privateMessage)
            };

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(emailTemplate, receivingUser.Email, emailParams.ToArray(), emailSubject);
        }

        private static async Task ProcessNewPhotoCommentEmailNotificationAsync(PhotoComment comment, Photo photo, User receivingUser, Post topic, Post reply = null)
        {
            if (!receivingUser.NewPhotoCommentNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new photo comment email notifications.");
                return;
            }

            // action it
            var recipient = await ForumServer.Instance.Users.GetUserAsync(receivingUser.Id);
            var commentator = await ForumServer.Instance.Users.GetUserAsync(comment.UserId);
            var synopsis = Utilities.GetContentSynopsis(comment.Text);

            // send an email
            var emailSubject = !string.IsNullOrEmpty(photo.Caption) ? $"New comment on photo: {photo.Caption}" : "New comment on photo";
            var photoUrl = Urls.GetPhotoUrl(photo.Id, topic, reply);

            var emailParams = new List<object>
            {
                recipient.UserName,
                topic.Subject,
                commentator.UserName,
                synopsis,
                photoUrl,
                ConfigurationManager.AppSettings["LB.Url"],
                Utilities.GetFileStoreIdForPhoto(photo)
            };

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.NewPhotoComment, receivingUser.Email, emailParams.ToArray(), emailSubject);
        }

        private static async Task ProcessNewPhotoCommentBatchUpdateEmailNotificationAsync(Notification notification, PhotoComment comment, Photo photo, User receivingUser, Post topic, Post reply = null)
        {
            if (!receivingUser.NewPhotoCommentNotifications)
            {
                Logging.LogDebug(typeof(NotificationController).FullName, "Users preferences do not allow new photo comment email notifications.");
                return;
            }

            // action it
            var recipient = await ForumServer.Instance.Users.GetUserAsync(receivingUser.Id);
            var commentator = await ForumServer.Instance.Users.GetUserAsync(comment.UserId);
            var synopsis = Utilities.GetContentSynopsis(comment.Text);

            // send an email
            var emailSubject = !string.IsNullOrEmpty(photo.Caption) ? $"New comment on photo: {photo.Caption}" : "New comment on photo";
            var photoUrl = Urls.GetPhotoUrl(photo.Id, topic, reply);

            var emailParams = new List<object>
            {
                recipient.UserName,
                notification.Occurances.ToString(),
                topic.Subject,
                commentator.UserName,
                synopsis,
                photoUrl,
                ConfigurationManager.AppSettings["LB.Url"],
                Utilities.GetFileStoreIdForPhoto(photo)
            };

            await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.NewPhotoCommentWithRollups, receivingUser.Email, emailParams.ToArray(), emailSubject);
        }
        #endregion
    }
}