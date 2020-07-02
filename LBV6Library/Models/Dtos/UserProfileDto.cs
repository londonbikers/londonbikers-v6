using System;

namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents a small amount of publicly available information for a user.
    /// </summary>
    public class UserProfileDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public DateTime Joined { get; set; }
        public UserStatus Status { get; set; }
        public string Tagline { get; set; }
        public string Biography { get; set; }
        public int TopicsCount { get; set; }
        public int RepliesCount { get; set; }
        public int PhotosCount { get; set; }
        public int ModerationsCount { get; set; }
        public int VisitsCount { get; set; }
        public string ProfileFileStoreId { get; set; }
        public string CoverFileStoreId { get; set; }
        public bool? Verified { get; set; }
        public UserPreferences Preferences { get; set; }

        public class UserPreferences
        {
            public bool NewTopicNotifications { get; set; }
            public bool NewReplyNotifications { get; set; }
            public bool NewMessageNotifications { get; set; }
            public bool NewPhotoCommentNotifications { get; set; }
            public bool ReceiveNewsletters { get; set; }
        }
    }
}