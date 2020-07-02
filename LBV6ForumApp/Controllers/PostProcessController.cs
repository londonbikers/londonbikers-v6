using LBV6Library;
using LBV6Library.Models;
using System;
using System.Configuration;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace LBV6ForumApp.Controllers
{
    /// <summary>
    /// Ensures that tasks that need to happen after user-instigated operations complete are properly sequenced.
    /// </summary>
    internal class PostProcessController
    {
        #region constructors
        internal PostProcessController(ForumServer forumServer)
        {
            // we use a reference to the forum server instead of accessing the normal ForumServer.Instance property
            // as the forum server is not fully initialised when this code is called and using this method would 
            // cause a stack-overflow as another forum server instance is instantiated despite the fact it follows 
            // a singleton pattern.

            forumServer.Posts.TopicCreated += ProcessTopicCreatedHandler;
            forumServer.Posts.TopicEdited += ProcessTopicEditedHandler;
            forumServer.Posts.TopicRemoved += ProcessTopicRemovedHandler;
            forumServer.Posts.TopicMoved += ProcessTopicMoveHandler;
            forumServer.Posts.TopicStickyChange += ProcessTopicStickyChangeHandler;
            forumServer.Posts.ReplyCreated += ProcessReplyCreatedHandler;
            forumServer.Posts.ReplyEdited += ProcessReplyEditedHandler;
            forumServer.Posts.ReplyRemoved += ProcessReplyRemovedHandler;

            forumServer.Photos.CommentAddedToPhoto += ProcessPhotoCommentCreatedHandler;

            forumServer.Users.UserCreated += ProcessUserCreatedHandler;
            forumServer.Users.UserUpdated += ProcessUserUpdatedHandler;
            forumServer.Users.UserRemoved += ProcessUserRemovedHandler;
        }
        #endregion

        #region posts
        private static void ProcessTopicCreatedHandler(Post topic)
        {
            if (HostingEnvironment.ApplicationHost != null)
                BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessTopicCreatedAsync(topic));
        }

        private static async Task ProcessTopicCreatedAsync(Post topic)
        {
            try
            {
                // sequence:
                // - index operations
                // - broadcast operations
                // - notification operations

                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIndexing"]))
                    ForumServer.Instance.Indexes.ProcessTopicCreated(topic);

                ForumServer.Instance.Broadcasts.ProcessTopicCreated(topic);

                await ForumServer.Instance.Notifications.ProcessTopicCreatedAsync(topic);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessTopicEditedHandler(Post topic)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessTopicEditedAsync(topic));
        }

        private static async Task ProcessTopicEditedAsync(Post topic)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                await ForumServer.Instance.Broadcasts.ProcessTopicEditedAsync(topic);

            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessReplyCreatedHandler(Post reply)
        {
            if (HostingEnvironment.ApplicationHost != null)
                BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessReplyCreatedAsync(reply));
        }

        private static async Task ProcessReplyCreatedAsync(Post reply)
        {
            try
            {
                // sequence:
                // - index operations
                // - broadcast operations
                // - notification operations

                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIndexing"]))
                    await ForumServer.Instance.Indexes.ProcessReplyCreationAsync(reply);

                await ForumServer.Instance.Broadcasts.ProcessReplyCreatedAsync(reply);

                await ForumServer.Instance.Notifications.ProcessReplyCreatedAsync(reply);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessReplyEditedHandler(Post reply)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessReplyEditedAsync(reply));
        }

        private static async Task ProcessReplyEditedAsync(Post reply)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                await ForumServer.Instance.Broadcasts.ProcessReplyEditedAsync(reply);

            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessTopicRemovedHandler(Post topic)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessTopicRemovedAsync(topic));
        }

        private static async Task ProcessTopicRemovedAsync(Post topic)
        {
            try
            {
                // sequence:
                // - index operations
                // - broadcast operations
                // - notification operations

                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIndexing"]))
                    await ForumServer.Instance.Indexes.ProcessTopicRemovalAsync(topic);

                ForumServer.Instance.Broadcasts.ProcessTopicRemoved(topic);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessTopicMoveHandler(Post topic, long oldForumId)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessTopicMoveAsync(topic, oldForumId));
        }

        private static async Task ProcessTopicMoveAsync(Post topic, long oldForumId)
        {
            try
            {
                // sequence:
                // - index operations
                // - broadcast operations
                // - notification operations

                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIndexing"]))
                    await ForumServer.Instance.Indexes.ProcessTopicMoveHandlerAsync(topic, oldForumId);

                await ForumServer.Instance.Broadcasts.ProcessTopicMovedAsync(topic, oldForumId);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessTopicStickyChangeHandler(Post topic)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessTopicStickyChangeAsync(topic));
        }

        private static async Task ProcessTopicStickyChangeAsync(Post topic)
        {
            try
            {
                // sequence:
                // - index operations
                // - broadcast operations
                // - notification operations

                if (bool.Parse(ConfigurationManager.AppSettings["LB.EnableIndexing"]))
                    await ForumServer.Instance.Indexes.ProcessTopicStickyChangeAsync(topic);

                await ForumServer.Instance.Broadcasts.ProcessTopicEditedAsync(topic);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessReplyRemovedHandler(Post reply)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcessReplyRemovedAsync(reply));
        }

        private static async Task ProcessReplyRemovedAsync(Post reply)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                await ForumServer.Instance.Broadcasts.ProcessReplyRemovedAsync(reply);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessPhotoCommentCreatedHandler(Guid photoId, long photoCommentId, Post topic, Post reply = null)
        {
            if (HostingEnvironment.ApplicationHost != null)
                BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ProcesPhotoCommentCreatedAsync(photoId, photoCommentId, topic, reply));
        }

        private static async Task ProcesPhotoCommentCreatedAsync(Guid photoId, long photoCommentId, Post topic, Post reply = null)
        {
            try
            {
                // sequence:
                // - notification operations

                await ForumServer.Instance.Notifications.ProcessPhotoCommentCreatedAsync(photoId, photoCommentId, topic, reply);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }
        #endregion

        #region users
        private static void ProcessUserCreatedHandler(User user)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(ct => ProcessUserCreated(user));
        }

        private static void ProcessUserCreated(User user)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                ForumServer.Instance.Broadcasts.ProcessUserCreated(user.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessUserUpdatedHandler(User user)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(ct => ProcessUserUpdated(user));
        }

        private static void ProcessUserUpdated(User user)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                ForumServer.Instance.Broadcasts.ProcessUserUpdated(user.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }

        private static void ProcessUserRemovedHandler(User user)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(ct => ProcessUserRemoved(user));
        }

        private static void ProcessUserRemoved(User user)
        {
            try
            {
                // sequence:
                // - broadcast operations
                // - notification operations

                ForumServer.Instance.Broadcasts.ProcessUserRemoved(user.Id);
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(PostProcessController).FullName, ex);
            }
        }
        #endregion
    }
}