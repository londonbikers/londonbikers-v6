using LBV6Library;
using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    internal class IndexController
    {
        #region members
        private static readonly int IndexSize = Convert.ToInt32(ConfigurationManager.AppSettings["LB.RecentTopicsIndexSize"]);
        #endregion

        internal void ProcessTopicCreated(Post topic)
        {
            // -- new topic: add to top of index underneath pinned posts, remove last from bottom if full
            if (topic.ForumId == null)
                return;

            var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
            lock (forum.RecentTopicsIndex)
            {
                // where is underneath the pinned posts, if any?
                var position = 0;
                var pinnedTopics = forum.RecentTopicsIndex.Where(q => q.Value).ToList();
                if (pinnedTopics.Any())
                    position = pinnedTopics.Count;

                forum.RecentTopicsIndex.Insert(position, new KeyValuePair<long, bool>(topic.Id, topic.IsSticky ?? false));
                Utilities.TrimList(forum.RecentTopicsIndex, IndexSize);
            }

            // update latest topic indexes, i.e. counts and positions
            // if post forum has no roles then increment all indexes
            if (forum.AccessRoles.Count == 0)
            {
                foreach (var index in ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes)
                {
                    index.Value.TotalPosts++;
                    index.Value.PostIds.Insert(0, topic.Id);
                    Utilities.TrimList(index.Value.PostIds, IndexSize);
                }
            }
            else
            {
                // work out which indexes can be updated, do this by working out which forum access roles match which index role combinations
                // ReSharper disable once LoopCanBePartlyConvertedToQuery - unreadable
                foreach (var key in ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes.Keys)
                {
                    // are any of the forum roles in the index role list?
                    var indexRoles = key.Split(';').ToList();
                    foreach (var indexRole in indexRoles)
                    {
                        if (!forum.AccessRoles.Any(q => q.Role.Equals(indexRole, StringComparison.InvariantCultureIgnoreCase)))
                            continue;

                        var index = ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes[key];
                        index.TotalPosts++;
                        index.PostIds.Insert(0, topic.Id);
                        Utilities.TrimList(index.PostIds, IndexSize);
                    }
                }
            }
        }

        internal async Task ProcessTopicRemovalAsync(Post topic)
        {
            // -- remove topic: remove from index, fill as needed
            if (topic.ForumId == null)
                return;

            var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);

            // don't do anything if the topic isn't in the index...
            if (!forum.RecentTopicsIndex.Any(q => q.Key.Equals(topic.Id)))
                return;

            // just rebuild the forum and latest topic indexes for simplicity as this is a low-occurrence event
            forum.RecentTopicsIndex = await ForumServer.Instance.Posts.GetTopicIdsForForumAsync(forum.Id, IndexSize, 0);
            ForumServer.Instance.Posts.InitialiseLatestTopicsIndexes();
        }

        internal async Task ProcessReplyCreationAsync(Post reply)
        {
            // -- new reply: move topic to top of forum and latest topic indexes
            if (reply.PostId != null)
            {
                var topic = await ForumServer.Instance.Posts.GetTopicAsync(reply.PostId.Value);
                if (topic.ForumId != null)
                {
                    // forum index
                    var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                    lock (forum.RecentTopicsIndex)
                    {
                        // if the topic isn't in the index, add it to the top, otherwise move it to the top
                        if (forum.RecentTopicsIndex.Any(q => q.Key.Equals(topic.Id))) forum.RecentTopicsIndex.RemoveAll(q => q.Key.Equals(topic.Id));

                        // where is underneath the pinned posts, if any?
                        var position = 0;
                        var pinnedTopics = forum.RecentTopicsIndex.Where(q => q.Value).ToList();
                        if (pinnedTopics.Any()) position = pinnedTopics.Count();

                        forum.RecentTopicsIndex.Insert(position, new KeyValuePair<long, bool>(topic.Id, topic.IsSticky ?? false));
                        Utilities.TrimList(forum.RecentTopicsIndex, IndexSize);
                    }

                    // latest topic indexes
                    if (forum.AccessRoles.Count == 0)
                    {
                        foreach (var index in ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes)
                        {
                            if (index.Value.PostIds.Contains(topic.Id)) index.Value.PostIds.Remove(topic.Id);
                            index.Value.PostIds.Insert(0, topic.Id);
                            Utilities.TrimList(index.Value.PostIds, IndexSize);
                        }
                    }
                    else
                    {
                        // work out which indexes can be updated, do this by working out which forum access roles match which index role combinations
                        // ReSharper disable once LoopCanBePartlyConvertedToQuery - unreadable
                        foreach (var key in ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes.Keys)
                        {
                            // are any of the forum roles in the index role list?
                            var indexRoles = key.Split(';').ToList();
                            foreach (var indexRole in indexRoles)
                            {
                                if (!forum.AccessRoles.Any(q => q.Role.Equals(indexRole, StringComparison.InvariantCultureIgnoreCase))) continue;
                                var index = ForumServer.Instance.Posts.RoleCombinationLatestTopicIndexes[key];
                                if (index.PostIds.Contains(topic.Id)) index.PostIds.Remove(topic.Id);
                                index.PostIds.Insert(0, topic.Id);
                                Utilities.TrimList(index.PostIds, IndexSize);
                            }
                        }
                    }
                }
            }
        }

        internal async Task ProcessTopicMoveHandlerAsync(Post topic, long oldForumId)
        {
            // -- move topic: remove from old index, fill out, add to new one, shrink if necessary
            var oldForum = ForumServer.Instance.Forums.GetForum(oldForumId);
            if (topic.ForumId == null) return;
            var newForum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);

            // just rebuild the forum indexes for simplicity as this is a low-occurrence event
            oldForum.RecentTopicsIndex = await ForumServer.Instance.Posts.GetTopicIdsForForumAsync(oldForum.Id, IndexSize, 0);
            newForum.RecentTopicsIndex = await ForumServer.Instance.Posts.GetTopicIdsForForumAsync(newForum.Id, IndexSize, 0);
        }

        internal async Task ProcessTopicStickyChangeAsync(Post topic)
        {
            if (topic.ForumId == null) return;
            var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);

            // don't do anything if the topic isn't in the index...
            if (!forum.RecentTopicsIndex.Any(q => q.Key.Equals(topic.Id)))
                return;

            // just rebuild the forum topic indexes for simplicity as this is a low-occurrence event
            // latest topic index doesn't need updating as this doesn't order items using sticky status
            forum.RecentTopicsIndex = await ForumServer.Instance.Posts.GetTopicIdsForForumAsync(forum.Id, IndexSize, 0);
        }
    }
}