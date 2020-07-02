using System;

namespace LBV6Library.Models
{
    public class Notification
    {
        #region identifiers
        /// <summary>
        /// The unique identifier for the Notification.
        /// </summary>
        public long Id { get; set; }
        #endregion
        
        #region foreign keys
        /// <summary>
        /// The unque identifier for any subscription that this notification may be related to.
        /// Notifications don't have to be related to a subscription, they can be associated with content directly, i.e. topics or private messages.
        /// </summary>
        public long? NotificationSubscriptionId { get; set; }

        /// <summary>
        /// If this notification relatest directly to content, then this needs to be supplied and should contain the unique identifier for the user receiving the notification.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The unique identifier for any content that this notification may be directly related to.
        /// Notifications don't have to be directly related to content, they can be associated with notification subscriptions for forums, for instance.
        /// This can still be populated for subscription-based notifications.
        /// </summary>
        public long? ContentId { get; set; }

        /// <summary>
        /// The unique identifier for any content that this notification may be directly related to.
        /// Notifications don't have to be directly related to content, they can be associated with notification subscriptions for photos, for instance.
        /// This can still be populated for subscription-based notifications.
        /// </summary>
        public Guid? ContentGuid { get; set; }
        #endregion

        /// <summary>
        /// The unique identifier for the content parent, i.e. a reply as the content would require a content parent that referenced the topic so the reply can be easily resolved.
        /// </summary>
        public long? ContentParentId { get; set; }

        /// <summary>
        /// For notifications about photo comments, this needs to be set as the ID for the comment.
        /// </summary>
        public long? CommentId { get; set; }

        /// <summary>
        /// When the notification was last updated, i.e. the occurance count was increased.
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        /// How many times a notification has occured for the notification subscription subject.
        /// When a notification has been read it is deleted and this counter essentially goes back to one for the next occurance of the notification.
        /// </summary>
        public int Occurances { get; set; }

        #region for content-based notifications
        /// <summary>
        /// If this notification relates directly to content, then this needs to be supplied, helping to tie the notification to the content and scenario intended.
        /// </summary>
        public NotificationType? ScenarioType { get; set; }
        #endregion

        #region constructors
        public Notification()
        {
            Updated = DateTime.UtcNow;
            Occurances = 1;
        }
        #endregion
    }
}