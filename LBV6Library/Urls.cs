using LBV6Library.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Web;

namespace LBV6Library
{
    public static class Urls
    {
        /// <summary>
        /// Gets the basic URL for the topic without any consideration for per-user customisations
        /// </summary>
        public static string GetTopicUrl(Post topic, bool absoluteUrl)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));
            if (topic.Id < 1)
                throw new ArgumentException("Topic is not persisted.");

            var prefix = absoluteUrl ? ConfigurationManager.AppSettings["LB.Url"] : null;
            return $"{prefix}/forums/posts/{topic.Id}/{Utilities.EncodeText(topic.Subject)}";
        }

        /// <summary>
        /// Gets a user-customised link to a topic, i.e. including any first-unread-reply linking.
        /// </summary>
        public static string GetTopicUrl(Post topic, bool absoluteUrl, long? lastPostSeenId)
        {
            // this does make an assumption about page size. the application default is 25 items per page.
            // if the ability for users to customises this in the future then page targeting will need to accomodate that.

            var baseUrl = GetTopicUrl(topic, absoluteUrl);
            if (!lastPostSeenId.HasValue)
                return baseUrl;

            var pageSize = int.Parse(ConfigurationManager.AppSettings["LB.DefaultPageSize"]);
            var readableReplies = topic.Replies.Where(q => q.Status != PostStatus.Removed).ToList();
            var firstUnreadReplyPosition = readableReplies.FindIndex(q => q.Id.Equals(lastPostSeenId.Value));

            // if we're not at the end of the replies, mark the next one the user has read as the first unread one
            // though there's a chance the user has read all the replies as they've seen the reply automatically
            // pushed to them.
            if (firstUnreadReplyPosition < readableReplies.Count - 1)
                firstUnreadReplyPosition++;

            var firstUnreadReplyId = readableReplies[firstUnreadReplyPosition].Id;

            var page = 1;
            var pageItemCount = 1;
            for (var i = 0; i < firstUnreadReplyPosition; i++)
            {
                if (pageItemCount == pageSize)
                {
                    page++;
                    pageItemCount = 1;
                    continue;
                }
                pageItemCount++;
            }

            if (page > 1)
                return baseUrl + $"?p={page}&hid={firstUnreadReplyId}";

            return baseUrl + $"?hid={firstUnreadReplyId}";
        }

        /// <summary>
        /// Gets a link to a topic for a specific reply
        /// </summary>
        public static string GetTopicUrl(Post topic, Post reply, bool absoluteUrl)
        {
            // this does make an assumption about page size. the application default is 25 items per page.
            // if the ability for users to customises this in the future then page targeting will need to accomodate that.

            var baseUrl = GetTopicUrl(topic, absoluteUrl);
            var pageSize = int.Parse(ConfigurationManager.AppSettings["LB.DefaultPageSize"]);
            var readableReplies = topic.Replies.Where(q => q.Status != PostStatus.Removed).ToList();
            var replyPosition = readableReplies.FindIndex(q => q.Id.Equals(reply.Id));
            

            var page = 1;
            var pageItemCount = 1;
            for (var i = 0; i < replyPosition; i++)
            {
                if (pageItemCount == pageSize)
                {
                    page++;
                    pageItemCount = 1;
                    continue;
                }
                pageItemCount++;
            }

            if (page > 1)
                return baseUrl + $"?p={page}&hid={reply.Id}";

            return baseUrl + $"?hid={reply.Id}";
        }

        /// <summary>
        /// Gets a link to a topic for a specific photo, causing the photo to be displayed when loaded.
        /// </summary>
        /// <param name="photoId">The unique identifier for the photo.</param>
        /// <param name="topic">The topic this photo resides in, whether that be in the topic itself or in a reply that belongs to the topic.</param>
        /// <param name="reply">If the photo belongs to a reply, please supply it.</param>
        public static string GetPhotoUrl(Guid photoId, Post topic, Post reply = null)
        {
            var baseUrl = GetTopicUrl(topic, true);
            var postId = reply?.Id ?? topic.Id;
            return $"{baseUrl}#gid={postId}&pid={photoId}";
        }

        public static string GetForumUrl(Forum forum)
        {
            return $"/forums/{forum.Id}/{Utilities.EncodeText(forum.Name)}";
        }

        public static string GetPrivateMessageAbsoluteUrl(PrivateMessage privateMessage)
        {
            return $"{ConfigurationManager.AppSettings["LB.Url"]}/intercom?h={privateMessage.PrivateMessageHeaderId}&m={privateMessage.Id}";
        }

        public static string GetUserProfileUrl(string username)
        {
            var url = "/" + HttpUtility.UrlEncode(username);

            // URLs with dots in need a trailing slash, otherwise IIS thinks it's a file being served
            if (url.Contains("."))
                url += "/";

            return url.ToLower();
        }
    }
}