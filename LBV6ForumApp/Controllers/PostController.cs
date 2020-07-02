using EntityFramework.BulkInsert.Extensions;
using EntityFramework.Extensions;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Containers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace LBV6ForumApp.Controllers
{
    public class PostController : IDisposable
    {
        #region accessors
        public Dictionary<string, PostIdsContainer> RoleCombinationPopularTopicIndexes { get; set; }
        public Dictionary<string, PostIdsContainer> RoleCombinationLatestTopicIndexes { get; set; }
        internal List<TopicView> TopicViewsBuffer { get; set; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private Timer GeneratePopularTopicsIndexesTimer { get; }
        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        private Timer TrimTopicViewsTimer { get; }
        #endregion

        #region constructors
        internal PostController()
        {
            RoleCombinationPopularTopicIndexes = new Dictionary<string, PostIdsContainer>();
            RoleCombinationLatestTopicIndexes = new Dictionary<string, PostIdsContainer>();
            InitialiseLatestTopicsIndexes();
            TopicViewsBuffer = new List<TopicView>();

            // some tasks need to run continuously every now and then...
            GeneratePopularTopicsIndexesTimer = new Timer(async delegate { await GeneratePopularTopicsIndexesAsync(); }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(10));
            TrimTopicViewsTimer = new Timer(async delegate { await TrimTopicViewsAsync(); }, null, TimeSpan.FromMilliseconds(0), TimeSpan.FromMinutes(10));
        }
        #endregion

        #region headers
        public async Task<PostsContainer> GetTopicHeaderAsync(long topicId, string userId)
        {
            var topic = await GetTopicAsync(topicId);
            if (topic == null)
                return null;

            var container = new PostsContainer();
            container.Posts.Add(topic);

            var seenBy = await GetTopicSeenByAsync(userId, topicId);
            if (seenBy != null)
                container.SeenBys.Add(seenBy);

            return container;
        }

        public async Task<PostsContainer> GetTopicsForForumAsync(long forumId, int limit, int startIndex, string userId)
        {
            var forum = ForumServer.Instance.Forums.GetForum(forumId);
            var topics = new PostsContainer { TotalPosts = forum.PostCount };
            if (topics.TotalPosts == 0)
                return null;

            #region get from index
            // where possible return results from the forum index, which is in memory
            if (forum.RecentTopicsIndex.Count - 1 > startIndex)
            {
                // how many items can we get out of the index?
                // can we take all the items wanted from the cache?

                var remainder = forum.RecentTopicsIndex.Count - startIndex;
                var itemsFromIndex = remainder >= limit ? limit : remainder;
                var indexIdKvps = forum.RecentTopicsIndex.Count > itemsFromIndex
                    ? forum.RecentTopicsIndex.GetRange(startIndex, itemsFromIndex).ToList()
                    : forum.RecentTopicsIndex;

                // in parallel get the seen-bys for the post ids from the database
                var indexTopicIds = indexIdKvps.Select(kvp => kvp.Key).ToList();
                var firstTasks = new Task<object>[2];
                firstTasks[0] = Task<object>.Factory.StartNew(() => GetTopicSeenBys(userId, indexTopicIds, topics));
                firstTasks[1] = Task<object>.Factory.StartNew(() => PopulatePostsContainerWithTopics(topics, indexIdKvps));

                try
                {
                    // ReSharper disable once CoVariantArrayConversion
                    Task.WaitAll(firstTasks);
                }
                catch (AggregateException ae)
                {
                    var flatException = ae.Flatten();
                    throw flatException;
                }
            }

            if (topics.Posts.Count == limit || forum.RecentTopicsIndex.Count == forum.PostCount)
                return topics;
            #endregion

            #region get from database
            // not all results could be sourced from the index, retrieve the rest from the database (excluding ones we already have from the index)
            // how many more do we need?
            limit = limit - topics.Posts.Count;
            startIndex = topics.Posts.Count > 0 ? topics.Posts.Count : 0;
            var databaseIds = await GetTopicIdsForForumAsync(forumId, limit, startIndex);

            // in parallel get the seen-bys for the post ids from the database
            var databaseTopicIds = databaseIds.Select(kvp => kvp.Key).ToList();
            var secondTasks = new Task<bool>[2];
            secondTasks[0] = Task<bool>.Factory.StartNew(() => GetTopicSeenBys(userId, databaseTopicIds, topics));
            secondTasks[1] = Task<bool>.Factory.StartNew(() => PopulatePostsContainerWithTopics(topics, databaseIds));

            try
            {
                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(secondTasks);
            }
            catch (AggregateException ae)
            {
                var flatException = ae.Flatten();
                throw flatException;
            }
            #endregion

            return topics;
        }

        public PostsContainer GetPopularTopics(TimeSpan timeSpan, int limit, int startIndex, string userId, List<string> roles)
        {
            PostIdsContainer index = null;

            #region find index
            if (roles == null || roles.Count == 0)
            {
                // anonymous user, find the key with no role and the right timespan
                // ReSharper disable once LoopCanBePartlyConvertedToQuery - optimisation is unreadable
                foreach (var key in RoleCombinationPopularTopicIndexes.Keys.Where(q => q.StartsWith("-")))
                {
                    var keyParts = key.Split('-');
                    var keyTimespan = TimeSpan.Parse(keyParts[1]);
                    if (timeSpan != keyTimespan)
                        continue;

                    index = RoleCombinationPopularTopicIndexes[key];
                    break;
                }
            }
            else
            {
                // find an index that matches the roles and timespan. they might not be in the same order so a role value comparison will be needed...
                // ReSharper disable once LoopCanBePartlyConvertedToQuery - optimisation is unreadable
                foreach (var key in RoleCombinationPopularTopicIndexes.Keys)
                {
                    var keyParts = key.Split('-');
                    var keyTimespan = TimeSpan.Parse(keyParts[1]);
                    if (timeSpan != keyTimespan)
                        continue;

                    // timespan matches, do the roles?
                    var keyRoles = keyParts[0].Split(';').ToList();
                    if (!Utilities.AreListContentsTheSame(roles, keyRoles))
                        continue;

                    // two role lists have the same values, i.e. there's no differences.
                    index = RoleCombinationPopularTopicIndexes[key];
                    break;
                }
            }

            if (index == null)
                throw new ArgumentException("roles don't match indexed roles.");
            #endregion

            // does the page request fall outside the index range? if so make the limit the index size less the startIndex (to preserve the approx desired page size)
            if (startIndex + limit > index.PostIds.Count)
                limit = index.PostIds.Count - startIndex;

            // then get the topics, which will come from the cache or database.
            // tried doing this in parallel but it screws the ordering
            var topics = new PostsContainer { TotalPosts = index.TotalPosts };
            var indexIdKvps = index.PostIds.GetRange(startIndex, limit);

            // in parallel get the seen-bys for the post ids from the database
            var firstTasks = new Task<object>[2];
            firstTasks[0] = Task<object>.Factory.StartNew(() => GetTopicSeenBys(userId, indexIdKvps, topics));
            firstTasks[1] = Task<object>.Factory.StartNew(() => PopulatePostsContainerWithTopics(topics, indexIdKvps));

            try
            {
                // ReSharper disable once CoVariantArrayConversion
                Task.WaitAll(firstTasks);
            }
            catch (AggregateException ae)
            {
                var flatException = ae.Flatten();
                throw flatException;
            }

            return topics;
        }

        public PostsContainer GetLatestTopics(int limit, int startIndex, string userId, List<string> roles)
        {
            // it's possible for the indexes not to be populated yet, i.e. on first request of an app restart
            // in this scenario let's fail gracefully and return null
            if (!RoleCombinationLatestTopicIndexes.ContainsKey(PostIndexType.Anonymous.ToString()))
                return null;

            var topics = new PostsContainer();
            var index = RoleCombinationLatestTopicIndexes[PostIndexType.Anonymous.ToString()];

            #region get from index
            // where possible return results from the forum index
            if (roles != null && roles.Count > 0)
            {
                // user is authenticated - determine which index is right for this role type
                // ReSharper disable once LoopCanBePartlyConvertedToQuery - optimisation is hard to read
                foreach (var key in RoleCombinationLatestTopicIndexes.Keys)
                {
                    var keyRoles = key.Split(';').ToList();
                    if (!Utilities.AreListContentsTheSame(keyRoles, roles))
                        continue;

                    index = RoleCombinationLatestTopicIndexes[key];
                    break;
                }
            }

            // assign the total post count from the index - even if we can't get any from it
            topics.TotalPosts = index.TotalPosts;

            if ((index.PostIds.Count - 1) > startIndex)
            {
                // how many items can we get out of the index?
                // can we take all the items wanted from the cache?
                var itemsFromIndex = (index.PostIds.Count - startIndex) >= limit ?
                    limit :
                    limit - (index.PostIds.Count - startIndex);

                var indexIds = index.PostIds.GetRange(startIndex, itemsFromIndex);

                // in parallel get the seen-bys for the post ids from the database
                var firstTasks = new Task<object>[2];
                firstTasks[0] = Task<object>.Factory.StartNew(() => GetTopicSeenBys(userId, indexIds, topics));
                firstTasks[1] = Task<object>.Factory.StartNew(() => PopulatePostsContainerWithTopics(topics, indexIds));

                try
                {
                    // ReSharper disable once CoVariantArrayConversion
                    Task.WaitAll(firstTasks);
                }
                catch (AggregateException ae)
                {
                    var flatException = ae.Flatten();
                    throw flatException;
                }
            }

            if (topics.Posts.Count == limit)
                return topics;
            #endregion

            #region get from database
            // not all results could be sourced from the index (if any)
            // how many more do we need?, adjust our start and finish point according to what we got out of the index
            limit = limit - topics.Posts.Count;
            startIndex = startIndex - (topics.Posts.Count - 1);

            // serialise the roles so we can use them in a string comparison in the query
            // we can't pass in a collection and compare them individually, which would obviously be the sensible way to do it but EF/SQL have their limits.
            var roleString = string.Empty;
            if (roles != null && roles.Count > 0)
                roleString = roles.Aggregate(roleString, (current, role) => current + (role + ";"));

            var idContainer = GetLatestTopicIds(limit, startIndex, roleString);

            // in parallel get the seen-bys for the post ids from the database
            var secondaryTasks = new Task[2];
            secondaryTasks[0] = Task<object>.Factory.StartNew(() => GetTopicSeenBys(userId, idContainer.PostIds, topics));
            secondaryTasks[1] = Task<object>.Factory.StartNew(() => PopulatePostsContainerWithTopics(topics, idContainer.PostIds));

            try
            {
                Task.WaitAll(secondaryTasks);
            }
            catch (AggregateException ae)
            {
                var flatException = ae.Flatten();
                throw flatException;
            }

            return topics;
            #endregion
        }
        #endregion

        #region replies
        /// <summary>
        /// Gets a selection (page) of replies to topic. Also records the last post seen for a user. Use this for authenticated requests.
        /// </summary>
        public async Task<PostsContainer> GetRepliesForTopicAsync(long topicId, int limit, int startIndex, string userId)
        {
            var replies = await GetRepliesForTopicAsync(topicId, limit, startIndex);
            var lastPostId = replies.Posts.Any() ? replies.Posts.Last().Id : topicId;
            await UpdateTopicSeenByAsync(topicId, lastPostId, userId);
            return replies;
        }

        public async Task<PostsContainer> GetRepliesForTopicAsync(long topicId, int limit, int startIndex)
        {
            var replies = new PostsContainer();
            var topic = await GetTopicAsync(topicId);
            replies.TotalPosts = topic.Replies?.Count(q => q.Status != PostStatus.Removed) ?? 0;

            if (topic.Replies != null && replies.TotalPosts > 0)
                replies.Posts = topic.Replies.Where(q => q.Status != PostStatus.Removed).Skip(startIndex).Take(limit).ToList();

            return replies;
        }
        #endregion

        #region cruds
        /// <summary>
        /// Retrieves a topic from the cache or database.
        /// </summary>
        public async Task<Post> GetTopicAsync(long id)
        {
            var topic = (Post)ForumServer.Instance.Cache.Get(Post.GetCacheKey(id));
            if (topic != null)
                return topic;

            using (var db = new ForumContext())
            {
                topic = await db.Posts.Where(q => q.Id.Equals(id) && q.Status != PostStatus.Removed).
                    Include(q => q.Replies).
                    Include("Replies.Photos").
                    Include("Replies.Photos.Comments").
                    Include(q => q.Attachments).
                    Include(q => q.ModerationHistory).
                    Include(q => q.Photos).
                    Include("Photos.Comments").
                    SingleOrDefaultAsync();

                return topic == null ? null : FinaliseTopic(topic);
            }
        }

        /// <summary>
        /// Retrieves a topic from the cache or database. 
        /// Also invalidates any notifications the user may have for the topic, so use this if there's a user involved.
        /// </summary>
        public async Task<Post> GetTopicAsync(long id, string userId)
        {
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ForumServer.Instance.Notifications.InvalidateTopicViewNotificationAsync(userId, id));
            return await GetTopicAsync(id);
        }

        /// <summary>
        /// Returns a single instance of a topic (Post object).
        /// </summary>
        /// <param name="id">The unique identifier for the topic.</param>
        public Post GetTopic(long id)
        {
            var topic = (Post)ForumServer.Instance.Cache.Get(Post.GetCacheKey(id));
            if (topic != null)
                return topic;

            using (var db = new ForumContext())
            {
                topic = db.Posts.Where(q => q.Id.Equals(id) && q.Status != PostStatus.Removed).
                    Include(q => q.Replies).
                    Include("Replies.Photos").
                    Include("Replies.Photos.Comments").
                    Include(q => q.Attachments).
                    Include(q => q.ModerationHistory).
                    Include(q => q.Photos).
                    Include("Photos.Comments").
                    SingleOrDefault();

                return topic == null ? null : FinaliseTopic(topic);
            }
        }

        public async Task<Post> GetTopicByLegacyIdAsync(int id)
        {
            var topic = ForumServer.Instance.Cache.GetLegacyPost(id);
            if (topic != null)
                return topic;

            // i did think about just querying the database for the new id using the legacy id and then using the existing get method, but this would result in another database call
            // and I want to minimise the load on the database as much as possible for performance and scalability reasons.
            using (var db = new ForumContext())
            {
                topic = await db.Posts.Where(q => q.LegacyPostId.HasValue && q.LegacyPostId.Value.Equals(id) && q.Status != PostStatus.Removed).
                    Include(q => q.Replies).
                    Include("Replies.Photos").
                    Include("Replies.Photos.Comments").
                    Include(q => q.Attachments).
                    Include(q => q.ModerationHistory).
                    Include(q => q.Photos).
                    Include("Photos.Comments").
                    SingleOrDefaultAsync();

                return topic == null ? null : FinaliseTopic(topic);
            }
        }

        public Post GetTopicByLegacyId(int id)
        {
            var topic = ForumServer.Instance.Cache.GetLegacyPost(id);
            if (topic != null)
                return topic;

            // i did think about just querying the database for the new id using the legacy id and then using the existing get method, but this would result in another database call
            // and I want to minimise the load on the database as much as possible for performance and scalability reasons.
            using (var db = new ForumContext())
            {
                topic = db.Posts.Where(q => q.LegacyPostId.HasValue && q.LegacyPostId.Value.Equals(id) && q.Status != PostStatus.Removed).
                    Include(q => q.Replies).
                    Include("Replies.Photos").
                    Include("Replies.Photos.Comments").
                    Include(q => q.Attachments).
                    Include(q => q.ModerationHistory).
                    Include(q => q.Photos).
                    Include("Photos.Comments").
                    SingleOrDefault();

                return topic == null ? null : FinaliseTopic(topic);
            }
        }

        public async Task<Post> GetTopicByLegacyGalleryIdAsync(int id)
        {
            var topic = ForumServer.Instance.Cache.GetLegacyPost(id);
            if (topic != null)
                return topic;

            using (var db = new ForumContext())
            {
                topic = await db.Posts.Where(q => q.LegacyGalleryId.HasValue && q.LegacyGalleryId.Value.Equals(id) && q.Status != PostStatus.Removed).
                    Include(q => q.Replies).
                    Include("Replies.Photos").
                    Include("Replies.Photos.Comments").
                    Include(q => q.Attachments).
                    Include(q => q.ModerationHistory).
                    Include(q => q.Photos).
                    Include("Photos.Comments").
                    SingleOrDefaultAsync();

                return topic == null ? null : FinaliseTopic(topic);
            }
        }

        public LegacyReplyContainer GetReplyByLegacyId(int id)
        {
            using (var db = new ForumContext())
            {
                var reply = db.Posts.SingleOrDefault(q => q.LegacyPostId.Value.Equals(id));
                if (reply?.PostId == null)
                    return null;

                var container = new LegacyReplyContainer
                {
                    ReplyId = reply.Id,
                    TopicId = reply.PostId.Value
                };

                return container;
            }
        }

        /// <summary>
        /// Creates a new or updates an existing post (topic or reply).
        /// </summary>
        /// <returns>
        /// Returns the new master version of the post, including any additions the initial transient post might not include.
        /// </returns>
        public async Task<Post> UpdatePostAsync(Post post)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (!string.IsNullOrEmpty(post.Content))
            {
                // ensure post content is tidy, remove html formatting nonsense
                post.Content = post.Content.Replace("<br></p>", "</p>");
                post.Content = post.Content.Replace("<p><br></p>", string.Empty);
                post.Content = post.Content.Replace("<p></p>", string.Empty);
                post.Content = post.Content.Trim();
            }

            if (post.Id < 1)
                return await CreateNewPostAsync(post);

            return await UpdateExistingPostAsync(post);
        }

        /// <summary>
        /// Increases the number of views on a topic by one.
        /// </summary>
        public void IncrementTopicViews(Post post, string userId = null, string ipAddress = null)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));

            if (!post.IsTopic)
                throw new ArgumentException("post is not a topic.");

            var topicView = new TopicView { UserId = userId, Ip = ipAddress, TopicId = post.Id };
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await ActionIncrementTopicViewsAsync(topicView));
        }

        public async Task<long?> GetLastReplySeenForTopicAsync(Post topic, User user)
        {
            using (var db = new ForumContext())
            {
                var seenBy = await db.TopicSeenBys.SingleOrDefaultAsync(q => q.PostId.Equals(topic.Id) && q.UserId.Equals(user.Id));
                return seenBy?.LastPostIdSeen;
            }
        }
        #endregion

        #region moderation
        /// <summary>
        /// Changes the forum a topic resides in and persists it.
        /// You must supply a reason which will be shared with the post author.
        /// </summary>
        public async Task ModeratorMoveTopicAsync(Post topic, long destinationForumId, string moderatorId, string reason)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));
            if (topic.Id < 1)
                throw new ArgumentException("Topic has not been persisted yet - cannot moderate it.");
            if (destinationForumId < 1)
                throw new ArgumentException("Destination forum id must be valid.");
            if (topic.PostId.HasValue)
                throw new ArgumentException("Cannot move a reply!");

            topic.ForumId = destinationForumId;
            var historyItem = new PostModerationHistoryItem
            {
                PostId = topic.Id,
                Justification = reason,
                ModeratorId = moderatorId,
                Type = ModerationType.Moved
            };

            topic.ModerationHistory.Add(historyItem);
            await UpdatePostAsync(topic);
        }

        /// <summary>
        /// Changes the status of a topic to closed and persists it.
        /// You must supply a reason which will be shared with the post author.
        /// </summary>
        public async Task ModeratorCloseTopicAsync(Post post, string moderatorId, string reason)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (post.Id < 1)
                throw new ArgumentException("Post has not been persisted yet - cannot moderate it.");
            if (post.PostId.HasValue)
                throw new ArgumentException("Cannot close a reply!");

            post.Status = PostStatus.Closed;
            var historyItem = new PostModerationHistoryItem
            {
                PostId = post.Id,
                Justification = reason,
                ModeratorId = moderatorId,
                Type = ModerationType.Closed
            };

            post.ModerationHistory.Add(historyItem);
            await UpdatePostAsync(post);
        }

        /// <summary>
        /// Changes the status of a topic to removed and persists it.
        /// You must supply a reason which will be shared with the post author.
        /// </summary>
        public async Task ModeratorRemoveTopicAsync(Post post, string moderatorId, string reason)
        {
            await ModeratorRemovePostAsync(post, moderatorId, reason);
        }

        /// <summary>
        /// Changes the status of a reply to removed and persists it.
        /// You must supply a reason which will be shared with the post author.
        /// Returns false if the reply wasn't found.
        /// </summary>
        public async Task<bool> ModeratorRemoveReplyAsync(long replyId, string moderatorId, string reason)
        {
            // we have to get a reply from the database as they're not cached independently and we don't know the parent post id at this stage
            // and don't want to complicate the API by passing it in.
            Post reply;
            using (var db = new ForumContext())
            {
                reply = await db.Posts.SingleOrDefaultAsync(q => q.Id.Equals(replyId));
                if (reply == null)
                    return false;
            }

            await ModeratorRemovePostAsync(reply, moderatorId, reason);
            return true;
        }

        /// <summary>
        /// Changes the status of a post to removed and persists it.
        /// You must supply a reason which will be shared with the post author.
        /// </summary>
        private async Task ModeratorRemovePostAsync(Post post, string moderatorId, string reason)
        {
            if (post == null)
                throw new ArgumentNullException(nameof(post));
            if (post.Id < 1)
                throw new ArgumentException("Post has not been persisted yet - cannot moderate it.");

            post.Status = PostStatus.Removed;
            var historyItem = new PostModerationHistoryItem
            {
                PostId = post.Id,
                Justification = reason,
                ModeratorId = moderatorId,
                Type = ModerationType.Removed
            };

            post.ModerationHistory.Add(historyItem);
            await UpdatePostAsync(post);
        }
        #endregion

        #region events
        public delegate void PostEventHandler(Post post);

        public event PostEventHandler TopicCreated;
        protected virtual void OnTopicCreated(EventArgs e, Post topic)
        {
            var handler = TopicCreated;
            handler?.Invoke(topic);
        }

        public event PostEventHandler TopicEdited;
        protected virtual void OnTopicEdited(EventArgs e, Post topic)
        {
            var handler = TopicEdited;
            handler?.Invoke(topic);
        }

        public delegate void TopicMoveEventHandler(Post post, long oldForumId);
        public event TopicMoveEventHandler TopicMoved;
        protected virtual void OnTopicMove(EventArgs e, Post topic, long oldForumId)
        {
            var handler = TopicMoved;
            handler?.Invoke(topic, oldForumId);
        }

        public event PostEventHandler TopicRemoved;
        protected virtual void OnTopicRemoved(EventArgs e, Post topic)
        {
            var handler = TopicRemoved;
            handler?.Invoke(topic);
        }

        public event PostEventHandler ReplyCreated;
        protected virtual void OnReplyCreated(EventArgs e, Post reply)
        {
            var handler = ReplyCreated;
            handler?.Invoke(reply);
        }

        public event PostEventHandler ReplyEdited;
        protected virtual void OnReplyEdited(EventArgs e, Post reply)
        {
            var handler = ReplyEdited;
            handler?.Invoke(reply);
        }

        public event PostEventHandler ReplyRemoved;
        protected virtual void OnReplyRemoved(EventArgs e, Post reply)
        {
            var handler = ReplyRemoved;
            handler?.Invoke(reply);
        }

        public event PostEventHandler TopicStickyChange;
        protected virtual void OnTopicStickyChange(EventArgs e, Post topic)
        {
            var handler = TopicStickyChange;
            handler?.Invoke(topic);
        }
        #endregion

        #region internal methods
        /// <summary>
        /// Produces a collection of strings that combine all of the possible permutations of roles, i.e.
        /// moderator, administrator, moderator;administrator, etc.
        /// </summary>
        internal List<string> GetRoleCombinations()
        {
            List<string> roles;
            using (var db = new ForumContext())
                roles = db.UserClaims.Where(w => w.ClaimType == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role").Select(q => q.ClaimValue).Distinct().ToList();

            return Utilities.TurnRolesIntoCombinationsCsv(roles);
        }

        /// <summary>
        /// Returns raw topic ids and pinned status for a forum. Used for building indexes and other internal uses. 
        /// </summary>
        internal async Task<List<KeyValuePair<long, bool>>> GetTopicIdsForForumAsync(long forumId, int limit, int startIndex)
        {
            using (var db = new ForumContext())
            {
                // get the id's for the topics from the database
                var results = await db.Posts.
                    Where(q => q.ForumId.Value.Equals(forumId) && q.Status != PostStatus.Removed).
                    OrderByDescending(q => q.IsSticky).
                    ThenByDescending(q => q.LastReplyCreated ?? q.Created).
                    Skip(startIndex).
                    Take(limit).
                    Select(q => new { q.Id, q.IsSticky }).
                    ToListAsync();

                return results.Select(result => new KeyValuePair<long, bool>(result.Id, result.IsSticky ?? false)).ToList();
            }
        }

        /// <summary>
        /// Returns raw topic ids for popular posts in a given time for a particular user role type.
        /// </summary>
        /// <param name="timeSpan">Over what time period the popular topics should be considered for.</param>
        /// <param name="limit">The number of results to return</param>
        /// <param name="startIndex">The position from within the results to return from, i.e. for paging.</param>
        /// <param name="roles">A colon-separated list of roles for the requesting user. Supply null or an empty string for anonymous users.</param>
        /// <returns>A collection of the topic ids found.</returns>
        internal async Task<PostIdsContainer> GetPopularTopicIdsAsync(TimeSpan timeSpan, int limit, int startIndex, string roles)
        {
            // the concept of popularity is one that will have to evolve as we introduce new metrics such as up-voting.
            // as things stand, popular posts can be defined by:
            // - posts with the most number of replies made during the timespan from now
            // - posts with the most views within the timespan from now

            var container = new PostIdsContainer();
            var from = DateTime.UtcNow - timeSpan;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                if (!string.IsNullOrEmpty(roles))
                {
                    command.CommandText = "GetPopularTopicsForUsersWithRoles";
                    command.Parameters.AddWithValue("@Roles", roles);
                }
                else
                {
                    command.CommandText = "GetPopularTopicsForUsersWithoutRoles";
                }

                command.Parameters.AddWithValue("@From", from);
                command.Parameters.AddWithValue("@Offset", startIndex);
                command.Parameters.AddWithValue("@PageSize", limit);
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // first result is the paged data
                    while (reader.Read())
                        container.PostIds.Add((long)reader["id"]);

                    // second result is the total item count across all pages
                    reader.NextResult();
                    while (reader.Read())
                        container.TotalPosts = (int)reader[0];
                }
            }

            return container;
        }

        /// <summary>
        /// Returns raw topic ids for latest posts for a particular user role type.
        /// </summary>
        /// <param name="limit">The number of results to return</param>
        /// <param name="startIndex">The position from within the results to return from, i.e. for paging.</param>
        /// <param name="roles">A colon-separated list of roles for the requesting user. Supply null or an empty string for anonymous users.</param>
        /// <returns>A collection of the topic ids found.</returns>
        internal PostIdsContainer GetLatestTopicIds(int limit, int startIndex, string roles)
        {
            var container = new PostIdsContainer();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandType = CommandType.StoredProcedure;
                if (!string.IsNullOrEmpty(roles))
                {
                    command.CommandText = "GetLatestTopicsForUsersWithRoles";
                    command.Parameters.AddWithValue("@Roles", roles);
                }
                else
                {
                    command.CommandText = "GetLatestTopicsForUsersWithoutRoles";
                }

                command.Parameters.AddWithValue("@Offset", startIndex);
                command.Parameters.AddWithValue("@PageSize", limit);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    // first result is the paged data
                    while (reader.Read())
                        container.PostIds.Add((long)reader["id"]);

                    // second result is the total item count across all pages
                    reader.NextResult();
                    while (reader.Read())
                        container.TotalPosts = (int)reader[0];
                }
            }

            return container;
        }

        internal void InitialiseLatestTopicsIndexes()
        {
            var indexSize = Convert.ToInt32(ConfigurationManager.AppSettings["LB.RecentTopicsIndexSize"]);
            RoleCombinationLatestTopicIndexes.Clear();
            foreach (var roleCombo in GetRoleCombinations())
            {
                var container = GetLatestTopicIds(indexSize, 0, roleCombo);
                RoleCombinationLatestTopicIndexes.Add(roleCombo, container);
            }

            // now do it again for anonymous users
            var container2 = GetLatestTopicIds(indexSize, 0, null);
            RoleCombinationLatestTopicIndexes.Add(PostIndexType.Anonymous.ToString(), container2);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Keeps track of what topics a user has seen and up to what point. This allows the UI to indicate posts which are new to the user.
        /// It also helps provide us with useful stats on post popularity.
        /// </summary>
        public async Task UpdateTopicSeenByAsync(long topicId, long lastPostIdSeen, string userId)
        {
            using (var db = new ForumContext())
            {
                var persist = false;
                var dbSeenBy = await db.TopicSeenBys.SingleOrDefaultAsync(q => q.PostId == topicId && q.UserId == userId);
                if (dbSeenBy == null)
                {
                    dbSeenBy = new TopicSeenBy { PostId = topicId, UserId = userId, LastPostIdSeen = lastPostIdSeen };
                    db.TopicSeenBys.Add(dbSeenBy);
                    persist = true;
                }
                else if (dbSeenBy.LastPostIdSeen < lastPostIdSeen)
                {
                    dbSeenBy.LastPostIdSeen = lastPostIdSeen;
                    persist = true;
                }

                if (persist)
                    await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Performs actions on a post after being retrieved from the database that need to happen
        /// before the post is ready for use elsewhere. Split out to avoid duplication in the
        /// different ways of retrieving the post from the database.
        /// </summary>
        /// <param name="post">The Post model straight from the database.</param>
        private static Post FinaliseTopic(Post post)
        {
            // make sure we're not being asked to retrieve a reply
            if (post == null || !post.IsTopic)
                return null;

            // order the includes as EF won't have
            post.Replies = post.Replies.OrderBy(q => q.Created).ToList();
            post.Attachments = post.Attachments.OrderBy(q => q.Created).ToList();
            post.ModerationHistory = post.ModerationHistory.OrderBy(q => q.Created).ToList();
            post.Photos = post.Photos.OrderBy(q => q.Position).ToList();

            // content post-processing
            post.Content = Utilities.ConvertContentLegacyUrls(post.Content);
            foreach (var reply in post.Replies)
                reply.Content = Utilities.ConvertContentLegacyUrls(reply.Content);

            // sort the photo comments
            foreach (var photo in post.Photos)
                foreach (var comment in photo.Comments)
                    SortPhotoComments(comment);

            if (post.Replies.Any())
            {
                foreach (var reply in post.Replies)
                {
                    foreach (var photo in reply.Photos)
                    {
                        foreach (var comment in photo.Comments)
                            SortPhotoComments(comment);
                    }
                }
            }

            // don't cache removed topics, they won't be displayed
            if (post.Status != PostStatus.Removed)
                ForumServer.Instance.Cache.Add(post);

            return post;
        }

        /// <summary>
        /// Recursively sorts all child photo comments in a photo comment.
        /// </summary>
        /// <param name="photoComment">The PhotoComment to sort the ChildComments on.</param>
        private static void SortPhotoComments(PhotoComment photoComment)
        {
            if (!photoComment.ChildComments.Any()) return;
            photoComment.ChildComments.Sort();
            foreach (var childComment in photoComment.ChildComments)
                SortPhotoComments(childComment);
        }

        /// <summary>
        /// Creates a new post. Broken out into own method due to size of combining creates and updates.
        /// </summary>
        /// <returns>
        /// Returns the new master version of the post, including any additions the initial transient post might not include.
        /// </returns>
        private async Task<Post> CreateNewPostAsync(Post transientPost)
        {
            if (!transientPost.PostId.HasValue && !transientPost.ForumId.HasValue)
                throw new ArgumentException("post has no parent transientPost or forum, so cannot be a reply or a topic!");

            using (var db = new ForumContext())
            {
                db.Posts.Add(transientPost);
                await db.SaveChangesAsync();
            }

            // shortcut: add any photos included in this create request
            if (transientPost.PhotoIdsToIncludeOnCreation != null)
                foreach (var photoId in transientPost.PhotoIdsToIncludeOnCreation)
                    await ForumServer.Instance.Photos.AddPhotoToPostAsync(transientPost, photoId);

            // update the forum stats
            var forumId = 0L;
            if (!transientPost.IsTopic && transientPost.PostId != null)
            {
                var topic = await GetTopicAsync(transientPost.PostId.Value);
                if (topic?.ForumId == null)
                    throw new Exception("Couldn't retrieve forum for reply topic.");

                forumId = topic.ForumId.Value;
            }
            else if (transientPost.IsTopic && transientPost.ForumId != null)
            {
                forumId = transientPost.ForumId.Value;
            }

            var forum = ForumServer.Instance.Forums.GetForum(forumId);
            forum.PostCount++;
            forum.LastUpdated = transientPost.Created;
            await ForumServer.Instance.Forums.UpdateForumAsync(forum);

            if (transientPost.IsTopic)
            {
                #region topic post-processing
                // move from the transient post to a finalised one that the rest of the application can use for when we fire event(s)
                ForumServer.Instance.Cache.Remove(transientPost.CacheKey);
                var topic = await GetTopicAsync(transientPost.Id);

                // should the user be subscribed to their topic?
                var user = await ForumServer.Instance.Users.GetUserAsync(topic.UserId);
                if (topic.SubscribeToTopic || user.NewTopicNotifications)
                    await ForumServer.Instance.Notifications.SubscribeToTopicAsync(user, topic);

                OnTopicCreated(EventArgs.Empty, topic);

                // return the new topic so the caller can continue working with the master version of the object.
                return topic;
                #endregion
            }

            if (transientPost.IsTopic || transientPost.PostId == null) return null;
            {
                #region reply post-processing
                // task:
                // - update topic stats
                // - update topic seen-by state
                // - add reply to cached topic
                // - subscribe user to topic if necessary
                // - fire event with complete reply

                // update topic stats
                var oldTopic = await GetTopicAsync(transientPost.PostId.Value);
                oldTopic.LastReplyCreated = transientPost.Created;

                // from this point the cache is up to date with the topic containing a version of the reply with photos if necessary
                await UpdatePostAsync(oldTopic);
                var currentTopic = await GetTopicAsync(oldTopic.Id);
                var reply = currentTopic.Replies.Single(q => q.Id.Equals(transientPost.Id));

                // update the seen-by state for the user and this topic - this is an isolated op
                await UpdateTopicSeenByAsync(currentTopic.Id, reply.Id, reply.UserId);

                // should the user be subscribed to replies to this topic?
                var user = await ForumServer.Instance.Users.GetUserAsync(reply.UserId);
                if (reply.SubscribeToTopic || user.NewReplyNotifications)
                    await ForumServer.Instance.Notifications.SubscribeToTopicAsync(user, currentTopic);

                OnReplyCreated(EventArgs.Empty, reply);

                // return the new reply so the caller can continue working with the master version of the object.
                return reply;
                #endregion
            }
        }

        /// <summary>
        /// Updates an existing post. Broken out into own method due to size of combining creates and updates.
        /// Does not update views. Use IncrementTopicViews() for that.
        /// </summary>
        /// <param name="transientPost">
        /// A temporary post object that contains updates to the authoritative transientPost. The transient transientPost is not considered authoritative, the database version is and it's
        /// this database post that is used to update the cache. This helps ensure that it's always clear what is the authoritative domain model with the most current state.
        /// </param>
        /// <returns>
        /// Returns the new master version of the post, including any additions the initial transient post might not include.
        /// </returns>
        private async Task<Post> UpdateExistingPostAsync(Post transientPost)
        {
            var isPostBeingEdited = false;
            var isPostBeingRemoved = false;
            var isTopicBeingMoved = false;
            var isStickyStatusChanging = false;
            long oldForumId = 0;
            long newForumId = 0;

            using (var db = new ForumContext())
            {
                var dbPost = await db.Posts
                    .Include(q => q.Photos)
                    .Include(q => q.Replies)
                    .Include("Replies.Photos")
                    .Include(q => q.Attachments)
                    .Include(q => q.ModerationHistory)
                    .SingleOrDefaultAsync(q => q.Id.Equals(transientPost.Id));

                if (dbPost == null)
                    throw new ArgumentException("Post not found in database.");

                #region post properties
                if (transientPost.ForumId.HasValue)
                {
                    // only applies to topics
                    if (!dbPost.ForumId.Equals(transientPost.ForumId) && dbPost.ForumId.HasValue)
                    {
                        isTopicBeingMoved = true;
                        oldForumId = dbPost.ForumId.Value;
                        newForumId = transientPost.ForumId.Value;
                        dbPost.ForumId = transientPost.ForumId.Value;
                    }

                    // subject is only updated for topics
                    if (transientPost.PostId.HasValue && !dbPost.Subject.Equals(transientPost.Subject))
                    {
                        dbPost.Subject = transientPost.Subject;
                        isPostBeingEdited = true;
                    }

                    if (!dbPost.IsSticky.Equals(transientPost.IsSticky))
                    {
                        isStickyStatusChanging = true;
                        dbPost.IsSticky = transientPost.IsSticky;
                    }
                }

                if (dbPost.IsTopic && (dbPost.Subject == null || !dbPost.Subject.Equals(transientPost.Subject)))
                {
                    dbPost.Subject = transientPost.Subject;
                    isPostBeingEdited = true;
                }

                if (string.IsNullOrEmpty(dbPost.Content) && !string.IsNullOrEmpty(transientPost.Content))
                {
                    dbPost.Content = transientPost.Content;
                    isPostBeingEdited = true;
                }

                if (!string.IsNullOrEmpty(dbPost.Content) && string.IsNullOrEmpty(transientPost.Content))
                {
                    dbPost.Content = null;
                    isPostBeingEdited = true;
                }

                if (!string.IsNullOrEmpty(dbPost.Content) && !string.IsNullOrEmpty(transientPost.Content) && !dbPost.Content.Equals(transientPost.Content))
                {
                    dbPost.Content = transientPost.Content;
                    isPostBeingEdited = true;
                }

#warning votes not handled by broadcast controller (forum views need to be updated)
                if (!dbPost.UpVotes.Equals(transientPost.UpVotes))
                    dbPost.UpVotes = transientPost.UpVotes;

                if (!dbPost.DownVotes.Equals(transientPost.DownVotes))
                    dbPost.DownVotes = transientPost.DownVotes;

                if (!dbPost.IsAnswer.Equals(transientPost.IsAnswer))
                    dbPost.IsAnswer = transientPost.IsAnswer;

                if (!dbPost.Status.Equals(transientPost.Status))
                {
                    if (dbPost.Status != PostStatus.Removed && transientPost.Status == PostStatus.Removed)
                        isPostBeingRemoved = true;

                    dbPost.Status = transientPost.Status;
                }

                if (!dbPost.LastReplyCreated.Equals(transientPost.LastReplyCreated))
                    dbPost.LastReplyCreated = transientPost.LastReplyCreated;

                // we only update this marker if the post content is being updated, i.e. subject or content.
                // also we give the user five minutes grace and don't record the edit within five minutes of them posting to do things like fix typos.
                if (isPostBeingEdited)
                {
                    var timeSpan = (DateTime.UtcNow - dbPost.Created);
                    if (timeSpan > TimeSpan.FromMinutes(5))
                        dbPost.EditedOn = DateTime.UtcNow;
                }
                #endregion

                #region persist new moderation items
                foreach (var mhi in transientPost.ModerationHistory.Where(q => q.Id < 1))
                {
                    db.PostModerationHistoryItems.Add(mhi);

                    // the topic author is not necessarily the author of the post we're moderating (i.e. if it's a reply by someone else).
                    var topic = transientPost.PostId.HasValue ? await GetTopicAsync(transientPost.PostId.Value) : transientPost;

                    // the author is the person who's post has been moderator, and who should be contacted.
                    var author = await ForumServer.Instance.Users.GetUserAsync(transientPost.UserId);
                    var emailParams = new List<object>
                    {
                        author.UserName,
                        Urls.GetTopicUrl(topic, true),
                        topic.Subject,
                        mhi.Type,
                        mhi.Justification
                    };

                    await ForumServer.Instance.Emails.SendTemplatedEmailAsync(EmailTemplate.PostModerationNotification, author.Email, emailParams.ToArray());
                }
                #endregion

                #region persist changes
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (DbEntityValidationException e)
                {
                    Logging.LogError(GetType().FullName, e);
                    foreach (var eve in e.EntityValidationErrors)
                    {
                        Logging.LogError(GetType().FullName, $"Entity of type \"{eve.Entry.Entity.GetType().Name}\" in state \"{eve.Entry.State}\" has the following validation errors:");
                        foreach (var ve in eve.ValidationErrors)
                            Logging.LogError(GetType().FullName, $"- Property: \"{ve.PropertyName}\", Error: \"{ve.ErrorMessage}\"");
                    }
                    throw;
                }
                #endregion

                #region update the cache
                // from this point the dbPost becomes the authoritative domain model for the post
                // prepare it as such and persist it in caches.

                // cache management - this will ensure that the changes in this model which might have come from the client
                // are synchronised with our domain models in the cache. This is necessary also for some operations further below to do with stat reconciliation.
                if (!dbPost.IsTopic && dbPost.PostId.HasValue)
                {
                    // replies
                    // swap-out the cached topic's version of this reply with this updated one
                    var topic = await GetTopicAsync(dbPost.PostId.Value);
                    lock (topic.Replies)
                    {
                        var index = topic.Replies.FindIndex(q => q.Id.Equals(dbPost.Id));
                        topic.Replies.RemoveAt(index);

                        // don't re-add it if it's been removed
                        if (dbPost.Status != PostStatus.Removed)
                            topic.Replies.Insert(index, dbPost);
                    }
                }
                else if (isPostBeingRemoved)
                {
                    ForumServer.Instance.Cache.Remove(dbPost.CacheKey);
                }
                else if (dbPost.IsTopic)
                {
                    // regular topic update - we need to transfer latest-state status from the cached post to the database post
                    // remove the current cached object first:
                    ForumServer.Instance.Cache.Remove(dbPost.CacheKey);

                    // then finalise the database post so it can become the authoritative domain model for this post.
                    // this will also add it back in to the cache.
                    FinaliseTopic(dbPost);
                }
                #endregion

                #region update forum stats
                if (isPostBeingRemoved)
                {
                    if (dbPost.IsTopic && dbPost.ForumId.HasValue)
                    {
                        // topic
                        await ForumServer.Instance.Forums.UpdateForumStatsAsync(dbPost.ForumId.Value);
                    }
                    else if (dbPost.PostId.HasValue)
                    {
                        // reply 
                        var topic = await GetTopicAsync(dbPost.PostId.Value);
                        if (topic.ForumId.HasValue)
                            await ForumServer.Instance.Forums.UpdateForumStatsAsync(topic.ForumId.Value);

                        // adjust the topic last reply date
                        var lastReply = topic.Replies.Where(q => q.Status != PostStatus.Removed).OrderByDescending(q => q.Created).FirstOrDefault();
                        if (lastReply != null)
                            topic.LastReplyCreated = lastReply.Created;
                        else
                            topic.LastReplyCreated = null;

                        await UpdatePostAsync(topic);
                    }
                }
                else if (isTopicBeingMoved)
                {
                    await ForumServer.Instance.Forums.UpdateForumStatsAsync(oldForumId);
                    await ForumServer.Instance.Forums.UpdateForumStatsAsync(newForumId);
                }
                #endregion

                #region fire events
                if (isPostBeingEdited)
                {
                    if (dbPost.IsTopic)
                        OnTopicEdited(EventArgs.Empty, dbPost);
                    else
                        OnReplyEdited(EventArgs.Empty, dbPost);
                }

                if (isPostBeingRemoved)
                {
                    if (dbPost.IsTopic)
                        OnTopicRemoved(EventArgs.Empty, dbPost);
                    else
                        OnReplyRemoved(EventArgs.Empty, dbPost);
                }

                if (isStickyStatusChanging)
                    OnTopicStickyChange(EventArgs.Empty, dbPost);

                if (isTopicBeingMoved)
                    OnTopicMove(EventArgs.Empty, dbPost, oldForumId);
                #endregion

                return dbPost;
            }
        }

        private static async Task<TopicSeenBy> GetTopicSeenByAsync(string userId, long topicId)
        {
            if (string.IsNullOrEmpty(userId))
                return null;

            using (var db = new ForumContext())
                return await db.TopicSeenBys.SingleOrDefaultAsync(q => q.PostId.Equals(topicId) && q.UserId.Equals(userId));
        }

        private static bool GetTopicSeenBys(string userId, ICollection<long> topicIds, PostsContainer container)
        {
            if (!topicIds.Any())
                return false;

            using (var db = new ForumContext())
                container.SeenBys.AddRange(db.TopicSeenBys.Where(q => q.UserId == userId && topicIds.Contains(q.PostId)).ToList());

            return true;
        }

        private bool PopulatePostsContainerWithTopics(PostsContainer container, IEnumerable<KeyValuePair<long, bool>> indexIds)
        {
            foreach (var kvp in indexIds)
                container.Posts.Add(GetTopic(kvp.Key));

            return true;
        }

        private bool PopulatePostsContainerWithTopics(PostsContainer container, IEnumerable<long> indexIds)
        {
            foreach (var id in indexIds)
                container.Posts.Add(GetTopic(id));

            return true;
        }

        private async Task ActionIncrementTopicViewsAsync(TopicView topicView)
        {
            try
            {
                Logging.LogDebug(GetType().FullName, "ActionIncrementTopicViews...");

                if (topicView.TopicId < 1)
                    throw new ArgumentException("Topic is not persisted yet.");

                TopicViewsBuffer.Add(topicView);
                if (TopicViewsBuffer.Count == int.Parse(ConfigurationManager.AppSettings["LB.TopicViewBufferSize"]))
                    await PersistTopicViewsAsync();
            }
            catch (Exception ex)
            {
                // catch here as this method is called from a fire-and-forget context with no exception bubbling possible.
                Logging.LogError(GetType().FullName, ex);
            }
        }

        /// <summary>
        /// Causes any buffered topic views to be flushed and persisted to the database.
        /// </summary>
        private async Task PersistTopicViewsAsync()
        {
            Logging.LogDebug(GetType().FullName, "PersistTopicViewsAsync...");

            var ids = TopicViewsBuffer.Select(q => q.TopicId).ToList();
            if (ids.Count == 0)
            {
                Logging.LogInfo(GetType().FullName, "No views to log.");
                return;
            }

            using (var db = new ForumContext())
            {
                // update the topic views, which gives us a life-time view on the popularity of a topic
                var totals = TopicViewsBuffer.GroupBy(view => view.TopicId).Select(group => new { TopicId = group.Key, Views = group.Count() }).ToList();
                foreach (var total in totals)
                {
                    var dbPost = await db.Posts.SingleAsync(q => q.Id.Equals(total.TopicId));
                    if (dbPost.Views.HasValue)
                        dbPost.Views += total.Views;
                    else
                        dbPost.Views = total.Views;
                }

                // bulk insert the topic-view objects, which give us granular data on views over time
                using (var ts = new TransactionScope())
                {
                    db.BulkInsert(TopicViewsBuffer, new BulkInsertOptions { EnableStreaming = true });
                    await db.SaveChangesAsync();
                    ts.Complete();
                }
            }

            TopicViewsBuffer.Clear();
            Logging.LogInfo(GetType().FullName, "Flushed topic views to database");
        }
        #endregion

        #region timer callbacks
        private static async Task GeneratePopularTopicsIndexesAsync()
        {
            try
            {
                Logging.LogDebug(typeof(PostController).FullName, "Building popular topics indexes");
                var indexSize = Convert.ToInt32(ConfigurationManager.AppSettings["LB.RecentTopicsIndexSize"]);
                var times = new List<TimeSpan> { TimeSpan.FromDays(1), TimeSpan.FromDays(7), TimeSpan.FromDays(30) };
                foreach (var roleCombo in ForumServer.Instance.Posts.GetRoleCombinations())
                {
                    // create the key for this index
                    // we combine the role combination string with a timespan the index is valid for, i.e.
                    // on the UI we provide three possible time-spans, 24 hours, 7 days and 3 months.
                    foreach (var time in times)
                    {
                        var key = roleCombo + "-" + time;
                        var container = await ForumServer.Instance.Posts.GetPopularTopicIdsAsync(time, indexSize, 0, roleCombo);
                        ForumServer.Instance.Posts.RoleCombinationPopularTopicIndexes[key] = container;
                    }
                }

                // now do it again for anonymous users
                foreach (var time in times)
                {
                    var key = "-" + time;
                    var container = await ForumServer.Instance.Posts.GetPopularTopicIdsAsync(time, indexSize, 0, null);
                    ForumServer.Instance.Posts.RoleCombinationPopularTopicIndexes[key] = container;
                }

                // broadcast the new topic list to clients viewing the popular topics page
                ForumServer.Instance.Broadcasts.ProcessPopularTopicsUpdate();

                Logging.LogInfo(typeof(PostController).FullName, "Updated popular topic indexes");
            }
            catch (Exception ex)
            {
                // catch here as this code is called during PostController construction and an exception there will total the app
                // plus it's on another thread so there's no exception bubbling.
                Logging.LogError(typeof(PostController).FullName, ex);
            }
        }

        /// <summary>
        /// We need to keep as many topic view logs in the database as our largest statistical period covers. At this time it's a month.
        /// This will delete old topic views that are older than that period.
        /// </summary>
        private static async Task TrimTopicViewsAsync()
        {
            try
            {
                Logging.LogInfo(typeof(PostController).FullName, "Trimming old TopicViews.");
                using (var db = new ForumContext())
                {
                    var aMonthAgo = DateTime.UtcNow.AddDays(-30);
                    await db.TopicViews.Where(q => q.When < aMonthAgo).DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                // catch here as this code is called during PostController construction and an exception there will total the app
                // plus it's on another thread so there's no exception bubbling.
                Logging.LogError(typeof(PostController).FullName, ex);
            }
        }
        #endregion

        public void Dispose()
        {
            Task.Run(PersistTopicViewsAsync).Wait();
        }
    }
}