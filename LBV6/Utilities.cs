using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using LBV6.Models.DTOs;
using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;

namespace LBV6
{
    /// <summary>
    /// Utility methods specific to the web-application go here.
    /// </summary>
    public static class Utilities
    {
        #region members
        public enum EventMessageType
        {
            Success,
            Information,
            Warning,
            Error
        }

        /// <summary>
        /// Represents a response that needs to be given to the user via a notification bar.
        /// </summary>
        public struct EventMessage
        {
            public EventMessageType Type { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }
        #endregion

        /// <summary>
        /// Checks to see if a users ip address is on the stopforumspam.com black-list.
        /// </summary>
        public static bool IsUserBlackListed(string ipAddress, string email)
        {
            var result = false;
            //var url = "http://www.stopforumspam.com/api?email=" + email;
            var url = "http://api.stopforumspam.org/api?email=" + email;

            if (!string.IsNullOrEmpty(ipAddress) && !ipAddress.Contains(":"))
                url += "&ip=" + ipAddress;

            try
            {
                var doc = XDocument.Load(url);
                result = doc.ToString().ToLowerInvariant().Contains("<appears>yes</appears");
                if (result)
                    Logging.LogInfo(string.Format("StopForumSpam API call: Email = {0}. IP = {1}. Result: black-listed.", email, ipAddress));
            }
            catch (Exception ex)
            {
                Logging.LogError(string.Format("Error with url: {0}, {1}", url, ex.Message));
            }

            return result;
        }

        /// <summary>
        /// Allows an event message to be shown to the user on the subsequent page view. Useful for if you need to
        /// perform a redirect but still wish to show a message at the resulting page.
        /// </summary>
        public static void SetEventMessage(string title, string content, EventMessageType type = EventMessageType.Information)
        {
            var response = new EventMessage { Title = title, Type = type, Content = content };
            HttpContext.Current.Session["EventMessage"] = response;
        }

        /// <summary>
        /// If an event message has been stored from the previous request then this will be returned.
        /// </summary>
        public static EventMessage? GetEventMessage()
        {
            if (HttpContext.Current.Session == null || HttpContext.Current.Session["EventMessage"] == null) return null;
            var response = (EventMessage)HttpContext.Current.Session["EventMessage"];
            HttpContext.Current.Session.Remove("EventMessage");
            return response;
        }

        #region categories
        public static List<CategoryDto> ConvertCategoriesToCategoryDtos(List<Category> categories)
        {
            // null dto means the user is not authorised to view the category
            return categories.Select(ConvertCategoryToCategoryDto).Where(dto => dto != null).ToList();
        }

        public static CategoryDto ConvertCategoryToCategoryDto(Category category)
        {
            var dto = new CategoryDto
            {
                Created = category.Created,
                Id = category.Id,
                Name = category.Name,
                Order = category.Order
            };

            foreach (var f in category.Forums.Where(IsUserAuthorisedForForum))
            {
                var forumDto = new ForumDto
                {
                    Id = f.Id,
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Created = f.Created,
                    Name = f.Name,
                    Order = f.Order,
                    PostCount = f.PostCount
                };

                if (f.LastUpdated.HasValue)
                    forumDto.LastUpdated = f.LastUpdated.Value;

                dto.Forums.Add(forumDto);
            }

            // if the user wasn't authorised to see any forums in this category then there will be no forums, either way
            // we don't want to transfer any empty categories to the client.
            return dto.Forums.Count == 0 ? null : dto;
        }

        public static List<CategorySimpleDto> ConvertCategoriesToCategorySimpleDtos(List<Category> categories)
        {
            // null dto means the user is not authorised to view the category
            return categories.Select(ConvertCategoryToCategorySimpleDto).Where(dto => dto != null).ToList();
        }

        public static CategorySimpleDto ConvertCategoryToCategorySimpleDto(Category category)
        {
            var dto = new CategorySimpleDto
            {
                Id = category.Id,
                Name = category.Name,
            };

            foreach (var f in category.Forums.Where(IsUserAuthorisedForForum))
            {
                dto.Forums.Add(new ForumSimpleDto
                {
                    Id = f.Id,
                    Name = f.Name,
                });
            }

            // if the user wasn't authorised to see any forums in this category then there will be no forums, either way
            // we don't want to transfer any empty categories to the client.
            return dto.Forums.Count == 0 ? null : dto;
        }
        #endregion

        #region forums
        public static ForumDto ConvertForumToForumDto(Forum forum)
        {
            var dto = new ForumDto
            {
                CategoryId = forum.CategoryId,
                CategoryName = ForumServer.Instance.Categories.Categories.Single(q => q.Id.Equals(forum.CategoryId)).Name,
                Created = forum.Created,
                Id = forum.Id,
                Name = forum.Name,
                Order = forum.Order,
                PostCount = forum.PostCount
            };

            if (forum.LastUpdated.HasValue)
                dto.LastUpdated = forum.LastUpdated.Value;

            return dto;
        }

        public static Forum ConvertForumDtoToForum(ForumDto dto)
        {
            return new Forum
            {
                CategoryId = dto.CategoryId,
                Created = dto.Created,
                Id = dto.Id,
                LastUpdated = dto.LastUpdated,
                Name = dto.Name,
                Order = dto.Order,
                PostCount = dto.PostCount
            };
        }
        #endregion

        #region posts
        public static List<TopicHeaderDto> ConvertTopicsToTopicHeaderDtos(IEnumerable<Post> posts)
        {
            var dtos = new List<TopicHeaderDto>();
            foreach (var p in posts)
            {
                if (!p.ForumId.HasValue)
                    throw new ArgumentException("One of the posts doesn't appear to be a topic. ForumId missing.");

                var author = ForumServer.Instance.Users.GetUser(p.UserId);
                var topicHeaderDto = new TopicHeaderDto
                {
                    Updated = p.LastReplyCreated ?? p.Created,
                    DownVotes = p.DownVotes.HasValue ? p.DownVotes.Value : 0,
                    ForumId = p.ForumId.Value,
                    ForumName = ForumServer.Instance.Forums.GetForum(p.ForumId.Value).Name,
                    Id = p.Id,
                    IsSticky = p.IsSticky.HasValue && p.IsSticky.Value,
                    ReplyCount = p.Replies != null ? p.Replies.Count(q => q.Status != PostStatus.Removed) : 0,
                    Subject = p.Subject,
                    UpVotes = p.UpVotes.HasValue ? p.UpVotes.Value : 0,
                    UserId = author.Id,
                    UserName = author.UserName,
                    UserProfilePhoto = GetUserProfilePhotoUrl(author),
                    StatusCode = (byte)p.Status
                };

                if (p.Replies != null && p.Replies.Count(q => q.Status != PostStatus.Removed) > 0)
                {
                    var lastReplyUser = ForumServer.Instance.Users.GetUser(p.Replies.Last().UserId);
                    topicHeaderDto.LastReplyByUserId = lastReplyUser.Id;
                    topicHeaderDto.LastReplyByUserName = lastReplyUser.UserName;
                    topicHeaderDto.LastReplyByUserProfilePhoto = GetUserProfilePhotoUrl(lastReplyUser);
                }

                dtos.Add(topicHeaderDto);
            }

            return dtos;
        }

        public static TopicDto ConvertTopicToTopicDto(Post post)
        {
            if (!post.ForumId.HasValue)
                throw new ArgumentException("Post doesn't seem to be a topic, ForumId missing.");

            var forum = ForumServer.Instance.Forums.GetForum(post.ForumId.Value);
            var category = ForumServer.Instance.Categories.Categories.Single(q => q.Id.Equals(forum.CategoryId));
            var author = ForumServer.Instance.Users.GetUser(post.UserId);

            var dto = new TopicDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Content = post.Content,
                Created = post.Created,
                DownVotes = post.DownVotes.HasValue ? post.DownVotes.Value : 0,
                ForumId = forum.Id,
                ForumName = forum.Name,
                Id = post.Id,
                IsSticky = post.IsSticky.HasValue && post.IsSticky.Value,
                Subject = post.Subject,
                UpVotes = post.UpVotes.HasValue ? post.UpVotes.Value : 0,
                UserId = author.Id,
                UserName = author.UserName,
                UserProfilePhoto = GetUserProfilePhotoUrl(author),
                UserTagline = author.Tagline,
                UserMemberSince = author.Created.Year,
                StatusCode = (byte)post.Status,
                EditedOn = post.EditedOn
            };

            if (post.Attachments != null)
            {
                dto.Attachments = new List<PostAttachmentDto>();
                foreach (var a in post.Attachments)
                    dto.Attachments.Add(new PostAttachmentDto { Url = GetPostAttachmentUrl(a) });
            }

            if (post.ModerationHistory != null)
            {
                dto.ModerationHistoryItems = new List<PostModerationHistoryItemDto>();
                foreach (var mhi in post.ModerationHistory)
                    dto.ModerationHistoryItems.Add(ConvertPostModerationHistoryToDto(mhi));
            }

            return dto;
        }

        public static Post ConvertTopicDtoToPost(TopicDto dto)
        {
            return new Post
            {
                Id = dto.Id,
                Created = dto.Created,
                Subject = dto.Subject,
                Content = dto.Content,
                ForumId = dto.ForumId,
                IsSticky = dto.IsSticky,
                UserId = dto.UserId
            };
        }

        public static List<ReplyDto> ConvertRepliesToReplyDtos(List<Post> posts)
        {
            return posts.Select(ConvertReplyToReplyDto).ToList();
        }

        public static ReplyDto ConvertReplyToReplyDto(Post post)
        {
            if (!post.PostId.HasValue)
                throw new ArgumentException("Post doesn't seem to be a reply. ParentPostId missing.");

            var author = ForumServer.Instance.Users.GetUser(post.UserId);
            var dto = new ReplyDto
            {
                Content = post.Content,
                Created = post.Created,
                DownVotes = post.DownVotes.HasValue ? post.DownVotes.Value : 0,
                Id = post.Id,
                ParentPostId = post.PostId.Value,
                UpVotes = post.UpVotes.HasValue ? post.UpVotes.Value : 0,
                UserId = author.Id,
                UserName = author.UserName,
                UserProfilePhoto = GetUserProfilePhotoUrl(author),
                UserMemberSince = author.Created.Year,
                UserTagline = author.Tagline,
                StatusCode = (byte)post.Status,
                EditedOn = post.EditedOn
            };

            if (post.Attachments != null)
            {
                dto.Attachments = new List<PostAttachmentDto>();
                foreach (var a in post.Attachments)
                    dto.Attachments.Add(new PostAttachmentDto { Url = GetPostAttachmentUrl(a) });
            }

            if (post.ModerationHistory != null)
            {
                dto.ModerationHistoryItems = new List<PostModerationHistoryItemDto>();
                foreach (var mhi in post.ModerationHistory)
                    dto.ModerationHistoryItems.Add(ConvertPostModerationHistoryToDto(mhi));
            }

            return dto;
        }

        public static Post ConvertReplyDtoToPost(ReplyDto dto)
        {
            return new Post
            {
                Id = dto.Id,
                Created = dto.Created,
                PostId = dto.ParentPostId,
                UserId = dto.UserId,
                Content = dto.Content,
                UpVotes = dto.UpVotes,
                DownVotes = dto.DownVotes,
                SubscribeToTopic = dto.SubscribeToTopic
            };
        }

        private static PostModerationHistoryItemDto ConvertPostModerationHistoryToDto(PostModerationHistoryItem pmhi)
        {
            var moderator = ForumServer.Instance.Users.GetUser(pmhi.ModeratorId);
            return new PostModerationHistoryItemDto
            {
                Created = pmhi.Created,
                ModeratorId = moderator.Id,
                ModeratorUserName = moderator.UserName,
                Reason = pmhi.Justification,
                Type = pmhi.Type.ToString()
            };
        }
        #endregion

        #region users
        public static UserProfileDto ConvertUserToUserProfileDto(User user)
        {
            var dto = new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Joined = user.Created.Date,
                Status = user.Status,
                Tagline = user.Tagline,
                Biography = user.Biography,
                VisitsCount = user.TotalVists,
                TopicsCount = user.TopicsCount.HasValue? user.TopicsCount.Value : 0,
                RepliesCount = user.RepliesCount.HasValue ? user.RepliesCount.Value : 0,
                PhotosCount = user.PhotosCount.HasValue ? user.PhotosCount.Value : 0,
                ModerationsCount = user.ModerationsCount.HasValue ? user.ModerationsCount.Value : 0,
                ProfilePhoto = GetUserProfilePhotoUrl(user),
                Verified = user.Verified
            };

            return dto;
        }

        public static UserProfileLightDto ConvertUserToUserProfileLightDto(User user)
        {
            var dto = new UserProfileLightDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Joined = user.Created.Date,
                Status = user.Status,
                Tagline = user.Tagline,
                ProfilePhoto = GetUserProfilePhotoUrl(user),
                Verified = user.Verified
            };

            return dto;
        }

        public static List<UserProfileLightDto> ConvertUsersToUserProfileLightDtos(List<User> users)
        {
            var dtos = new List<UserProfileLightDto>();
            foreach (var user in users)
                dtos.Add(ConvertUserToUserProfileLightDto(user));
            return dtos;
        }

        public static User ConvertUserProfileLightDtoToUser(UserProfileLightDto dto)
        {
            return ForumServer.Instance.Users.GetUser(dto.Id);
        }

        public static string GetUseProfileUrl(string username)
        {
            var url = "/" + HttpUtility.UrlEncode(username);

            // url's with dots in need a trailing slash, otherwise IIS thinks it's a file being served
            if (url.Contains("."))
                url += "/";

            return url.ToLower();
        }

        /// <summary>
        /// Returns a relative url to a users profile picture if they have one, otherwise null. Resize commands should be querystringed on to the end on the client side.
        /// </summary>
        public static string GetUserProfilePhotoUrl(User user)
        {
            if (!user.ProfilePhotoVersion.HasValue)
                return null;

            return "/_amedia/" + user.Created.Year + "/" + user.Created.Month + "/" + user.Created.Day + "/" + user.Id + "_" + user.ProfilePhotoVersion.Value + ".jpg";
        }

        /// <summary>
        /// Returns the local path to the folder where the user's profile photo is or is to be stored.
        /// </summary>
        public static string GetUserProfilePictureLocation(User user)
        {
            return string.Format("{0}\\{1}\\{2}\\{3}", ConfigurationManager.AppSettings["LB.MediaRoot"], user.Created.Year, user.Created.Month, user.Created.Day);
        }
        #endregion

        #region attachments
        public static string GetPostAttachmentUrl(PostAttachment attachment)
        {
            return string.Format("{0}/_amedia/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["LB.Url"], attachment.Created.Year, attachment.Created.Month, attachment.Created.Day, attachment.Filename.ToLower());
        }

        public static string GetPrivateMessageAttachmentUrl(PrivateMessageAttachment attachment)
        {
            return string.Format("{0}/_amedia/{1}/{2}/{3}/{4}", ConfigurationManager.AppSettings["LB.Url"], attachment.Created.Year, attachment.Created.Month, attachment.Created.Day, attachment.Filename.ToLower());
        }
        #endregion

        #region access-control
        public static bool IsUserAuthorisedForForum(Forum forum)
        {
            return forum.AccessRoles.Count == 0 ||
                forum.AccessRoles.Any(ar => HttpContext.Current.User.Identity.IsAuthenticated && HttpContext.Current.User.IsInRole(ar.Role));
        }
        #endregion

        #region private messages
        public static PrivateMessageHeader ConvertPrivateMessageHeaderDtoToPrivateMessageHeader(PrivateMessageHeaderDto dto)
        {
            var header = new PrivateMessageHeader();
            header.LastMessageCreated = dto.LastMessageCreated;
            header.Id = dto.Id;

            foreach (var dtoUser in dto.Users)
            {
                var headerUser = new PrivateMessageHeaderUser();
                headerUser.Id = dtoUser.Id;
                headerUser.Added = dtoUser.Added;
                headerUser.UserId = dtoUser.User.Id;
                header.Users.Add(headerUser);
            }

            return header;
        }

        public static PrivateMessageHeaderDto ConvertPrivateMessageHeaderToPrivateMessageHeaderDto(PrivateMessageHeader header, string currentUserId)
        {
            var dto = new PrivateMessageHeaderDto();
            dto.LastMessageCreated = header.LastMessageCreated;
            dto.Id = header.Id;

            foreach (var headerUser in header.Users)
            {
                var dtoUser = new PrivateMessageHeaderUserDto();
                dtoUser.Id = headerUser.Id;
                dtoUser.Added = headerUser.Added;
                dtoUser.User = ConvertUserToUserProfileLightDto(ForumServer.Instance.Users.GetUser(headerUser.UserId));
                dto.Users.Add(dtoUser);

                // does the currently logged-in user have any unread messages in this header?
                if (headerUser.UserId.Equals(currentUserId))
                    dto.HasUnreadMessagesForCurrentUser = headerUser.HasUnreadMessages;
            }

            return dto;
        }

        public static List<PrivateMessageHeaderDto> ConvertPrivateMessageHeadersToPrivateMessageHeaderDtos(List<PrivateMessageHeader> headers, string currentUserId)
        {
            var dtos = new List<PrivateMessageHeaderDto>();
            foreach (var header in headers)
                dtos.Add(ConvertPrivateMessageHeaderToPrivateMessageHeaderDto(header, currentUserId));
            return dtos;
        }

        public static PrivateMessage ConvertPrivateMessageDtoToPrivateMessage(PrivateMessageDto dto)
        {
            var message = new PrivateMessage();
            message.Id = dto.Id;
            message.PrivateMessageHeaderId = dto.PrivateMessageHeaderId;
            message.UserId = dto.UserId;
            message.Content = dto.Content;
            message.Created = dto.Created;
            return message;
        }

        public static PrivateMessageDto ConvertPrivateMessageToPrivateMessageDto(PrivateMessage message)
        {
            var dto = new PrivateMessageDto();
            dto.Id = message.Id;
            dto.PrivateMessageHeaderId = message.PrivateMessageHeaderId;
            dto.Created = message.Created;
            dto.UserId = message.UserId;
            dto.Content = message.Content;

            var user = ForumServer.Instance.Users.GetUser(message.UserId);
            dto.UserName = user.UserName;
            dto.UserProfilePhoto = GetUserProfilePhotoUrl(user);

            if (message.ReadBy != null)
            { 
                foreach (var readBy in message.ReadBy)
                {
                    var readByDto = new PrivateMessageReadByDto();
                    readByDto.Id = readBy.Id;
                    readByDto.When = readBy.When;
                    readByDto.UserId = readBy.UserId;

                    var readByUser = ForumServer.Instance.Users.GetUser(readBy.UserId);
                    readByDto.UserName = readByUser.UserName;

                    dto.ReadBy.Add(readByDto);
                }
            }

            if (message.Attachments != null)
                foreach (var attachment in message.Attachments)
                    dto.Attachments.Add(new PostAttachmentDto { Url = GetPrivateMessageAttachmentUrl(attachment) });

            return dto;
        }

        public static List<PrivateMessageDto> ConvertPrivateMessagesToPrivateMessageDtos(List<PrivateMessage> messages)
        {
            var dtos = new List<PrivateMessageDto>();
            foreach (var message in messages)
                dtos.Add(ConvertPrivateMessageToPrivateMessageDto(message));
            return dtos;
        }
        #endregion

        /// <summary>
        /// Causes a 301 Moved Permanently response to be sent to the client. 
        /// </summary>
        /// <param name="url">The URL to permanently redirect to.</param>
        public static void RedirectPermanently(string url)
        {
            HttpContext.Current.Response.Status = "301 Moved Permanently";
            HttpContext.Current.Response.StatusCode = 301;
            HttpContext.Current.Response.AppendHeader("Location", url);
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// Causes a 404 Not Found response to be returned.
        /// </summary>
        public static void Return404()
        {
            HttpContext.Current.Response.Status = "404 Not Found";
            HttpContext.Current.Response.StatusCode = 404;
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }
    }
}