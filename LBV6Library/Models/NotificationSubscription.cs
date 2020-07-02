namespace LBV6Library.Models
{
    /// <summary>
    /// Defines a subscription to a notification, i.e. something that handles an event and results in an action being performed.
    /// </summary>
    public class NotificationSubscription
    {
        /// <summary>
        /// The unique identifier for the subscription.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The unique identifier for the user subscribing to the notification.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The type of notification this subscription is for.
        /// </summary>
        public NotificationType Type { get; set; }
        /// <summary>
        /// The ID of the subject causing the notification, i.e. the forum id, the topic id, the private-message-header id, etc.
        /// </summary>
        public long SubjectId { get; set; }
    }
}