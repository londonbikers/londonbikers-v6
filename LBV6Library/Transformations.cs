using LBV6Library.Models;
using LBV6Library.Models.Containers;
using LBV6Library.Models.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LBV6Library
{
    public static class Transformations
    {
        #region private messages
        public static async Task<List<PrivateMessageHeaderDto>> ConvertPrivateMessageHeadersToPrivateMessageHeaderDtosAsync(List<PrivateMessageHeader> headers, string currentUserId, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var headerDtos = new List<PrivateMessageHeaderDto>();
            foreach (var header in headers)
                headerDtos.Add(await ConvertPrivateMessageHeaderToPrivateMessageHeaderDtoAsync(header, currentUserId, getUserAsyncDelegate));
            return headerDtos;
        }

        public static async Task<PrivateMessageHeaderDto> ConvertPrivateMessageHeaderToPrivateMessageHeaderDtoAsync(PrivateMessageHeader header, string currentUserId, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var dto = new PrivateMessageHeaderDto { LastMessageCreated = header.LastMessageCreated, Id = header.Id };
            foreach (var headerUser in header.Users)
            {
                var dtoUser = new PrivateMessageHeaderUserDto
                {
                    Id = headerUser.Id,
                    Added = headerUser.Added,
                    User = ConvertUserToUserProfileLightDto(await getUserAsyncDelegate(headerUser.UserId))
                };
                dto.Users.Add(dtoUser);

                // does the currently logged-in user have any unread messages in this header?
                if (headerUser.UserId.Equals(currentUserId))
                    dto.HasUnreadMessagesForCurrentUser = headerUser.HasUnreadMessages;
            }

            return dto;
        }

        public static PrivateMessageHeader ConvertPrivateMessageHeaderDtoToPrivateMessageHeader(PrivateMessageHeaderDto dto)
        {
            var header = new PrivateMessageHeader { LastMessageCreated = dto.LastMessageCreated, Id = dto.Id };

            foreach (var headerUser in dto.Users.Select(dtoUser => new PrivateMessageHeaderUser
            {
                Id = dtoUser.Id,
                Added = dtoUser.Added,
                UserId = dtoUser.User.Id
            }))
            {
                header.Users.Add(headerUser);
            }

            return header;
        }

        public static async Task<List<PrivateMessageDto>> ConvertPrivateMessagesToPrivateMessageDtosAsync(List<PrivateMessage> messages, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var messageDtos = new List<PrivateMessageDto>();
            foreach (var message in messages)
                messageDtos.Add(await ConvertPrivateMessageToPrivateMessageDtoAsync(message, getUserAsyncDelegate));
            return messageDtos;
        }

        public static async Task<PrivateMessageDto> ConvertPrivateMessageToPrivateMessageDtoAsync(PrivateMessage message, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var dto = new PrivateMessageDto
            {
                Id = message.Id,
                PrivateMessageHeaderId = message.PrivateMessageHeaderId,
                Created = message.Created,
                UserId = message.UserId,
                Content = message.Content,
                Type = message.Type.ToString()
            };

            var user = await getUserAsyncDelegate(message.UserId);
            dto.UserName = user.UserName;
            dto.ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user);

            if (message.ReadBy != null)
            {
                foreach (var readBy in message.ReadBy)
                {
                    var readByDto = new PrivateMessageReadByDto
                    {
                        Id = readBy.Id,
                        When = readBy.When,
                        UserId = readBy.UserId
                    };

                    var readByUser = await getUserAsyncDelegate(readBy.UserId);
                    readByDto.UserName = readByUser.UserName;

                    dto.ReadBy.Add(readByDto);
                }
            }

            // we're transitioning attachments to photos and will start calling them photos from the dto
            // the actual domain model can change later if we want it to.
            if (message.Attachments == null) return dto;
            foreach (var attachment in message.Attachments)
                dto.Photos.Add(new PostAttachmentDto {
                    FilestoreId = Utilities.GetFileStoreIdForPrivateMessageAttachment(attachment),
                    Width = attachment.Width,
                    Height = attachment.Height
                });

            return dto;
        }

        public static PrivateMessage ConvertPrivateMessageDtoToPrivateMessage(PrivateMessageDto dto)
        {
            var message = new PrivateMessage
            {
                Id = dto.Id,
                PrivateMessageHeaderId = dto.PrivateMessageHeaderId,
                UserId = dto.UserId,
                Content = dto.Content,
                Created = dto.Created,
                Type = PrivateMessageType.Message
            };
            return message;
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
                TopicsCount = user.TopicsCount ?? 0,
                RepliesCount = user.RepliesCount ?? 0,
                PhotosCount = user.PhotosCount ?? 0,
                ModerationsCount = user.ModerationsCount ?? 0,
                Verified = user.Verified,
                Preferences = new UserProfileDto.UserPreferences
                {
                    NewMessageNotifications = user.NewMessageNotifications,
                    NewReplyNotifications = user.NewReplyNotifications,
                    NewTopicNotifications = user.NewTopicNotifications,
                    ReceiveNewsletters = user.ReceiveNewsletters
                },
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user),
                CoverFileStoreId = Utilities.GetFileStoreIdForCoverPhoto(user)
            };

            return dto;
        }

        public static UserProfileExtendedDto ConvertUserToUserProfileExtendedDto(User user)
        {
            var dto = new UserProfileExtendedDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Joined = user.Created,
                Status = user.Status,
                Tagline = user.Tagline,
                Biography = user.Biography,
                VisitsCount = user.TotalVists,
                TopicsCount = user.TopicsCount ?? 0,
                RepliesCount = user.RepliesCount ?? 0,
                PhotosCount = user.PhotosCount ?? 0,
                ModerationsCount = user.ModerationsCount ?? 0,
                Verified = user.Verified,
                Preferences = new UserProfileDto.UserPreferences
                {
                    NewMessageNotifications = user.NewMessageNotifications,
                    NewReplyNotifications = user.NewReplyNotifications,
                    NewTopicNotifications = user.NewTopicNotifications,
                    ReceiveNewsletters = user.ReceiveNewsletters
                },
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user),
                CoverFileStoreId = Utilities.GetFileStoreIdForCoverPhoto(user),

                // privileged attributes
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Logins = user.Logins,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Occupation = user.Occupation
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
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user),
                Verified = user.Verified
            };

            return dto;
        }

        public static List<UserProfileLightDto> ConvertUsersToUserProfileLightDtos(List<User> users)
        {
            return users.Select(ConvertUserToUserProfileLightDto).ToList();
        }

        public static UserProfileLightExtendedDto ConvertUserToUserProfileLightExtendedDto(User user)
        {
            var dto = new UserProfileLightExtendedDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Joined = user.Created.Date,
                Status = user.Status,
                Tagline = user.Tagline,
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user),
                Verified = user.Verified,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Created = user.Created,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TopicsCount = user.TopicsCount ?? 0,
                RepliesCount = user.RepliesCount ?? 0,
                Logins = user.Logins
            };

            return dto;
        }

        public static List<UserProfileLightExtendedDto> ConvertUsersToUserProfileLightExtendedDtos(List<User> users)
        {
            return users.Select(ConvertUserToUserProfileLightExtendedDto).ToList();
        }
        #endregion

        #region forums
        public static ForumDto ConvertForumToForumDto(Forum forum, List<Category> categories)
        {
            var dto = new ForumDto
            {
                CategoryId = forum.CategoryId,
                CategoryName = categories.Single(q => q.Id.Equals(forum.CategoryId)).Name,
                Created = forum.Created,
                Id = forum.Id,
                Name = forum.Name,
                Description = forum.Description,
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
                Description = dto.Description,
                Order = dto.Order,
                PostCount = dto.PostCount
            };
        }

        public static ForumAccessRole ConvertForumRoleDtoToForumAccessRole(ForumRoleDto dto)
        {
            return new ForumAccessRole
            {
                Id = dto.Id,
                ForumId = dto.ForumId,
                Role = dto.Role
            };
        }

        public static ForumPostRole ConvertForumRoleDtoToForumPostRole(ForumRoleDto dto)
        {
            return new ForumPostRole
            {
                Id = dto.Id,
                ForumId = dto.ForumId,
                Role = dto.Role
            };
        }

        public static ForumRoleDto ConvertForumAccessRoleToForumRoleDto(ForumAccessRole forumAccessRole)
        {
            return new ForumRoleDto
            {
                ForumId = forumAccessRole.ForumId,
                Id = forumAccessRole.Id,
                Role = forumAccessRole.Role
            };
        }

        public static ForumRoleDto ConvertForumPostRoleToForumRoleDto(ForumPostRole forumPostRole)
        {
            return new ForumRoleDto
            {
                ForumId = forumPostRole.ForumId,
                Id = forumPostRole.Id,
                Role = forumPostRole.Role
            };
        }
        #endregion

        #region posts
        public static Post ConvertReplyDtoToPost(ReplyDto dto)
        {
            var p = new Post
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

            if (dto.Photos != null)
                foreach (var dtoPhoto in dto.Photos)
                    p.Photos.Add(ConvertPhotoDtoToPhoto(dtoPhoto));

            if (dto.PhotoIdsToIncludeOnCreation == null)
                return p;

            foreach (var dtoPhotoId in dto.PhotoIdsToIncludeOnCreation)
                p.PhotoIdsToIncludeOnCreation.Add(dtoPhotoId);

            return p;
        }

        public static Post ConvertTopicDtoToPost(TopicDto dto)
        {
            var p = new Post
            {
                Id = dto.Id,
                Created = dto.Created,
                Subject = dto.Subject,
                Content = dto.Content,
                ForumId = dto.ForumId,
                IsSticky = dto.IsSticky,
                UserId = dto.UserId
            };

            if (dto.Photos != null)
                foreach (var dtoPhoto in dto.Photos)
                    p.Photos.Add(ConvertPhotoDtoToPhoto(dtoPhoto));

            if (dto.PhotoIdsToIncludeOnCreation == null)
                return p;

            foreach (var dtoPhotoId in dto.PhotoIdsToIncludeOnCreation)
                p.PhotoIdsToIncludeOnCreation.Add(dtoPhotoId);

            return p;
        }

        public static async Task<List<TopicHeaderDto>> ConvertTopicsToTopicHeaderDtosAsync(PostsContainer container, bool isUserAuthenticated, GetUserAsyncDelegate getUserAsyncDelegate, GetForumDelegate getForumDelegate)
        {
            var dtos = new List<TopicHeaderDto>();
            foreach (var topic in container.Posts)
            {
                // seen odd cases where container.SeenBys is null
                var topicSeenBy = container.SeenBys?.SingleOrDefault(q => q.PostId.Equals(topic.Id));
                dtos.Add(
                    await
                        ConvertTopicToTopicHeaderDtoAsync(topic, topicSeenBy, isUserAuthenticated, getUserAsyncDelegate,
                            getForumDelegate));
            }

            return dtos;
        }

        public static async Task<TopicHeaderDto> ConvertTopicToTopicHeaderDtoAsync(Post topic, TopicSeenBy topicSeenBy, bool isUserAuthenticated, GetUserAsyncDelegate getUserAsyncDelegate, GetForumDelegate getForumDelegate)
        {
            if (!topic.ForumId.HasValue)
                throw new ArgumentException("One of the posts doesn't appear to be a topic. ForumId missing.");

            var dto = new TopicHeaderDto
            {
                Updated = topic.LastReplyCreated ?? topic.Created,
                DownVotes = topic.DownVotes ?? 0,
                ForumId = topic.ForumId.Value,
                ForumName = getForumDelegate(topic.ForumId.Value).Name,
                Id = topic.Id,
                IsSticky = topic.IsSticky.HasValue && topic.IsSticky.Value,
                ReplyCount = topic.Replies?.Count(q => q.Status != PostStatus.Removed) ?? 0,
                Subject = topic.Subject,
                UpVotes = topic.UpVotes ?? 0,
                StatusCode = (byte)topic.Status,
                HasAttachments =
                    topic.Attachments.Count > 0 ||
                    (topic.Replies != null && topic.Replies.Any(q => q.Attachments != null && q.Attachments.Count > 0)),
                HasPhotos =
                    topic.Photos.Count > 0 ||
                    (topic.Replies != null && topic.Replies.Any(q => q.Photos != null && q.Photos.Count > 0)),
                ProminentUsers = await GetTopicProminentUsersAsync(topic, getUserAsyncDelegate)
            };

            if (isUserAuthenticated)
            {
                dto.IsNew = topicSeenBy == null;
                if (topicSeenBy != null && topic.Replies != null && topic.Replies.Count > 0 &&
                    topicSeenBy.LastPostIdSeen < topic.Replies.Last().Id)
                    dto.HasNewReplies = true; // seen and there are new replies
                else
                    dto.HasNewReplies = false;

                // need to know:
                // how many replies there are
                // what the first unread post is
                // what index position this post is in
                // ... then the client can work this out

                if (topic.Replies != null && topicSeenBy != null && dto.HasNewReplies.Value)
                {
                    var visibleReplies = topic.Replies.Where(q => q.Status != PostStatus.Removed).ToList();
                    dto.FirstUnreadReplyPosition = 0;

                    // is the last post seen in the visible replies?
                    if (visibleReplies.Any(q => q.Id.Equals(topicSeenBy.LastPostIdSeen)))
                    {
                        var lastReadIndex = visibleReplies.FindIndex(q => q.Id.Equals(topicSeenBy.LastPostIdSeen));

                        // is the last read index within the remainder of the list?
                        if (lastReadIndex > -1 && lastReadIndex + 1 < visibleReplies.Count)
                            dto.FirstUnreadReplyPosition = lastReadIndex + 1;

                        dto.FirstUnreadReplyId = visibleReplies[dto.FirstUnreadReplyPosition.Value].Id;
                    }
                    else
                    {
                        Logging.LogWarning(typeof(Transformations).FullName, "Couldn't find a last post seen in visible replies.");
                    }
                }
            }

            if (topic.Replies == null || topic.Replies.Count(q => q.Status != PostStatus.Removed) <= 0)
                return dto;

            return dto;
        }

        public static async Task<TopicDto> ConvertTopicToTopicDtoAsync(Post post, List<Category> categories, GetUserAsyncDelegate getUserAsyncDelegate, GetForumDelegate getForumDelegate)
        {
            if (!post.IsTopic || !post.ForumId.HasValue)
                throw new ArgumentException("Post is not a topic!");

            var forum = getForumDelegate(post.ForumId.Value);
            var category = categories.Single(q => q.Id.Equals(forum.CategoryId));
            var author = await getUserAsyncDelegate(post.UserId);

            var dto = new TopicDto
            {
                CategoryId = category.Id,
                CategoryName = category.Name,
                Content = post.Content,
                Created = post.Created,
                DownVotes = post.DownVotes ?? 0,
                ForumId = forum.Id,
                ForumName = forum.Name,
                Id = post.Id,
                IsSticky = post.IsSticky.HasValue && post.IsSticky.Value,
                Subject = post.Subject,
                UpVotes = post.UpVotes ?? 0,
                UserId = author.Id,
                UserName = author.UserName,
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(author),
                UserTagline = author.Tagline,
                UserMemberSince = author.Created.Year,
                StatusCode = (byte)post.Status,
                EditedOn = post.EditedOn,
                IsGallery = category.IsGalleryCategory,
                ProtectPhotos = forum.ProtectTopicPhotos
            };

            if (post.Attachments != null)
            {
                dto.Attachments = new List<PostAttachmentDto>();
                foreach (var attachment in post.Attachments)
                    dto.Attachments.Add(new PostAttachmentDto { Url = Utilities.GetPostAttachmentUrl(attachment) });
            }

            if (post.ModerationHistory == null) return dto;
            dto.ModerationHistoryItems = new List<PostModerationHistoryItemDto>();
            foreach (var mhi in post.ModerationHistory)
                dto.ModerationHistoryItems.Add(await ConvertPostModerationHistoryToDtoAsync(mhi, getUserAsyncDelegate));

            if (post.Photos == null)
                return dto;

            foreach (var photo in post.Photos)
                dto.Photos.Add(await ConvertPhotoToPhotoDtoAsync(photo, getUserAsyncDelegate));

            return dto;
        }

        public static async Task<List<ReplyDto>> ConvertRepliesToReplyDtosAsync(List<Post> posts, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var replyDtos = new List<ReplyDto>();
            foreach (var post in posts)
                replyDtos.Add(await ConvertReplyToReplyDtoAsync(post, getUserAsyncDelegate));

            return replyDtos;
        }

        public static async Task<ReplyDto> ConvertReplyToReplyDtoAsync(Post post, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            if (!post.PostId.HasValue)
                throw new ArgumentException("Post doesn't seem to be a reply. ParentPostId missing.");

            var author = await getUserAsyncDelegate(post.UserId);
            var dto = new ReplyDto
            {
                Content = post.Content,
                Created = post.Created,
                DownVotes = post.DownVotes ?? 0,
                Id = post.Id,
                ParentPostId = post.PostId.Value,
                UpVotes = post.UpVotes ?? 0,
                UserId = author.Id,
                UserName = author.UserName,
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(author),
                UserMemberSince = author.Created.Year,
                UserTagline = author.Tagline,
                StatusCode = (byte)post.Status,
                EditedOn = post.EditedOn
            };

            if (post.Attachments != null)
            {
                dto.Attachments = new List<PostAttachmentDto>();
                foreach (var a in post.Attachments)
                    dto.Attachments.Add(new PostAttachmentDto { Url = Utilities.GetPostAttachmentUrl(a) });
            }

            if (post.ModerationHistory == null) return dto;
            dto.ModerationHistoryItems = new List<PostModerationHistoryItemDto>();
            foreach (var mhi in post.ModerationHistory)
                dto.ModerationHistoryItems.Add(await ConvertPostModerationHistoryToDtoAsync(mhi, getUserAsyncDelegate));

            if (post.Photos == null)
                return dto;

            foreach (var p in post.Photos)
                dto.Photos.Add(await ConvertPhotoToPhotoDtoAsync(p, getUserAsyncDelegate));

            return dto;
        }

        private static async Task<PostModerationHistoryItemDto> ConvertPostModerationHistoryToDtoAsync(PostModerationHistoryItem pmhi, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var moderator = await getUserAsyncDelegate(pmhi.ModeratorId);
            return new PostModerationHistoryItemDto
            {
                Created = pmhi.Created,
                ModeratorId = moderator.Id,
                ModeratorUserName = moderator.UserName,
                Reason = pmhi.Justification,
                Type = pmhi.Type.ToString()
            };
        }

        /// <summary>
        /// Identifies the most prominent users within a thread, i.e. the author, those who contribute the most and the last person to reply.
        /// </summary>
        private static async Task<List<ProminentUserDto>> GetTopicProminentUsersAsync(Post topic, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            const int maxProlificUsers = 4;
            var pus = new List<ProminentUserDto>();

            // add the author
            var author = await getUserAsyncDelegate(topic.UserId);
            pus.Add(new ProminentUserDto { UserName = author.UserName, ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(author), Reason = "Poster" });

            // identify the top few most prolific contributors to the topic
            var userCounts = new Dictionary<string, int>();
            foreach (var r in topic.Replies)
            {
                if (!userCounts.ContainsKey(r.UserId))
                    userCounts.Add(r.UserId, 1);
                else
                    userCounts[r.UserId]++;
            }

            // take the top most prolific user id's which doesn't include the author
            var prolificUsers = userCounts.Where(q => q.Key != author.Id).OrderByDescending(q => q.Value).Take(maxProlificUsers).Select(q => q.Key).ToList();

            foreach (var uid in prolificUsers)
            {
                var u = await getUserAsyncDelegate(uid);
                pus.Add(new ProminentUserDto { UserName = u.UserName, ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(u), Reason = "Top Contributor" });
            }

            if (topic.Replies.Count <= 0)
                return pus;

            // add the last person to reply (make sure they're not already in the list)
            var lastUser = await getUserAsyncDelegate(topic.Replies.Last().UserId);
            if (!pus.Any(q => q.UserName.Equals(lastUser.UserName)))
                pus.Add(new ProminentUserDto
                {
                    UserName = lastUser.UserName,
                    ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(lastUser),
                    Reason = "Last Poster"
                });
            else
                pus.Single(q => q.UserName.Equals(lastUser.UserName)).Reason += "/Last Poster";

            return pus;
        }
        #endregion

        #region photos
        public static async Task<PhotoDto> ConvertPhotoToPhotoDtoAsync(Photo photo, GetUserAsyncDelegate getUserAsyncDelegate)
        {
            var photoDto = new PhotoDto
            {
                Id = photo.Id,
                FilestoreId = Utilities.GetFileStoreIdForPhoto(photo),
                Caption = photo.Caption,
                Credit = photo.Credit,
                Position = photo.Position,
                Width = photo.Width,
                Height = photo.Height,
                Created = photo.Created
            };

            if (photo.PostId.HasValue)
                photoDto.PostId = photo.PostId.Value;

            foreach (var comment in photo.Comments)
            {
                var user = await getUserAsyncDelegate(comment.UserId);
                var photoCommentDto = new PhotoCommentDto
                {
                    Id = comment.Id,
                    PhotoId = photo.Id,
                    Created = comment.Created,
                    Text = comment.Text,
                    UserId = comment.UserId,
                    UserName = user.UserName,
                    ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user)
                };

                photoDto.Comments.Add(photoCommentDto);
            }

            return photoDto;
        }

        public static Photo ConvertPhotoDtoToPhoto(PhotoDto photoDto)
        {
            var p = new Photo
            {
                Id = photoDto.Id,
                Caption = photoDto.Caption,
                Position = photoDto.Position
            };

            return p;
        }
        #endregion

        #region notifications
        public static async Task<NotificationDto> ConvertNotificationToNotificationDtoAsync(
            Notification notification,
            GetTopicAsyncDelegate getTopicAsyncDelegate,
            GetForumDelegate getForumDelegate,
            GetLastReplySeenForTopicAsyncDelegate getLastReplySeenForTopicAsyncDelegate,
            GetUserAsyncDelegate getUserAsyncDelegate,
            string userId)
        {
            var dto = new NotificationDto
            {
                Id = notification.Id,
                Occurances = notification.Occurances,
                ScenarioType = notification.ScenarioType
            };

            // get the notification content
            switch (notification.ScenarioType)
            {
                case NotificationType.NewTopic:
                    #region new topic
                    if (notification.ContentId.HasValue)
                    {
                        var topic = await getTopicAsyncDelegate(notification.ContentId.Value);
                        if (topic.ForumId.HasValue)
                        {
                            var forum = getForumDelegate(topic.ForumId.Value);
                            dto.ParentName = forum.Name;
                        }

                        dto.Users.Add(ConvertUserToUserProfileLightDto(await getUserAsyncDelegate(topic.UserId)));
                        dto.ContentName = topic.Subject;
                        dto.ContentUrl = Urls.GetTopicUrl(topic, false);
                        dto.IsOwnContent = topic.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        throw new NotSupportedException("This notification should have content!");
                    }
                    #endregion
                    break;

                case NotificationType.NewTopicReply:
                    #region new topic reply
                    if (notification.ContentId.HasValue)
                    {
                        var user = await getUserAsyncDelegate(notification.UserId);
                        var topic = await getTopicAsyncDelegate(notification.ContentId.Value);
                        var lastReplyIdSeen = await getLastReplySeenForTopicAsyncDelegate(topic, user);

                        // find out who created the new reply
                        var increment = lastReplyIdSeen < topic.Replies.Last().Id ? 1 : 0;
                        var newReplyIndex = topic.Replies.FindIndex(q => q.Id.Equals(lastReplyIdSeen)) + increment;
                        var newReply = topic.Replies[newReplyIndex];

                        dto.Users.Add(ConvertUserToUserProfileLightDto(await getUserAsyncDelegate(newReply.UserId)));
                        dto.ContentName = topic.Subject;
                        dto.ContentUrl = Urls.GetTopicUrl(topic, false, lastReplyIdSeen);
                        dto.IsOwnContent = topic.UserId == userId;
                    }
                    else
                    {
                        throw new NotSupportedException("This notification should have content!");
                    }
                    #endregion
                    break;

                case NotificationType.NewPhotoComment:
                    #region new photo comment
                    if (notification.ContentGuid.HasValue && notification.CommentId.HasValue && notification.ContentParentId.HasValue)
                    {
                        var topic = await getTopicAsyncDelegate(notification.ContentParentId.Value);
                        var photo = Utilities.FindPhoto(topic, notification.ContentGuid.Value);
                        var comment = Utilities.FindPhotoComment(topic, notification.CommentId.Value);
                        var reply = notification.ContentId.HasValue
                            ? topic.Replies.Single(q => q.Id.Equals(notification.ContentId.Value))
                            : null;

                        var photoUrl = Urls.GetPhotoUrl(photo.Id, topic, reply);

                        dto.Users.Add(ConvertUserToUserProfileLightDto(await getUserAsyncDelegate(comment.UserId)));
                        dto.ContentName = topic.Subject;
                        dto.ContentUrl = photoUrl;
                        dto.ContentFilestoreId = Utilities.GetFileStoreIdForPhoto(photo);
                        dto.IsOwnContent = photo.UserId == userId;
                    }
                    else
                    {
                        throw new NotSupportedException("This notification should have a content guid, id and a content parent!");
                    }
                    #endregion
                    break;

                case NotificationType.NewPrivateMessage:
                    #region new private message
                    Logging.LogDebug(typeof(Transformations).FullName, "Not handling Intercom notifications by the notifications menu at this time.");
                    #endregion
                    break;

                case NotificationType.ReplyModeration:
                    #region reply moderation
                    if (notification.ContentId.HasValue && notification.ContentParentId.HasValue)
                    {
                        var topic = await getTopicAsyncDelegate(notification.ContentParentId.Value);
                        var reply = topic.Replies.SingleOrDefault(q => q.Id.Equals(notification.ContentId.Value));
                        if (reply == null)
                            throw new InvalidOperationException("Content id was not a reply id!");

                        dto.ContentName = topic.Subject;
                        dto.ContentUrl = Urls.GetTopicUrl(topic, reply, false);
                        dto.IsOwnContent = reply.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        throw new NotSupportedException("This notification should have a content parent id and content id!");
                    }
                    #endregion
                    break;

                case NotificationType.TopicModeration:
                    #region topic moderation
                    if (notification.ContentId.HasValue)
                    {
                        var topic = await getTopicAsyncDelegate(notification.ContentId.Value);
                        dto.ContentName = topic.Subject;
                        dto.ContentUrl = Urls.GetTopicUrl(topic, false);
                        dto.IsOwnContent = topic.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase);
                    }
                    else
                    {
                        throw new NotSupportedException("This notification should have a content id!");
                    }
                    #endregion
                    break;

                case null:
                    #region null
                    Logging.LogError(typeof(Transformations).FullName, "Null passed as a notification scenario type.");
                    #endregion
                    break;

                default:
                    #region default
                    Logging.LogError(typeof(Transformations).FullName, "Unsupported notification scenario type.");
                    throw new ArgumentOutOfRangeException();
                    #endregion
            }

            return dto;
        }

        public static async Task<List<NotificationDto>> ConvertNotificationsToNotificationDtosAsync(
            List<Notification> notifications,
            GetTopicAsyncDelegate getTopicAsyncDelegate,
            GetForumDelegate getForumDelegate,
            GetLastReplySeenForTopicAsyncDelegate getLastReplySeenForTopicAsyncDelegate,
            GetUserAsyncDelegate getUserAsyncDelegate,
            string userId)
        {
            var dtos = new List<NotificationDto>();
            foreach (var notification in notifications)
                dtos.Add(await ConvertNotificationToNotificationDtoAsync(notification, getTopicAsyncDelegate, getForumDelegate, getLastReplySeenForTopicAsyncDelegate, getUserAsyncDelegate, userId));

            return dtos;
        }
        #endregion

        #region delegates
        public delegate Task<User> GetUserAsyncDelegate(string userId);
        public delegate Forum GetForumDelegate(long forumId);
        public delegate Task<Post> GetTopicAsyncDelegate(long topicId);
        public delegate Task<long?> GetLastReplySeenForTopicAsyncDelegate(Post topic, User user);
        #endregion
    }
}