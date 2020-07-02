namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents a small sub-set of user profile information that can be used to express the current logged-in user.
    /// </summary>
    public class UserProfileSelfDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public UserPreferences Preferences { get; set; }
        public bool IsModerator { get; set; }
        public int UnreadMessagesCount { get; set; }
        public int NonMessageNotificationsCount { get; set; }

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