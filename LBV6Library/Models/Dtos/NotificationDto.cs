using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class NotificationDto
    {
        #region accessors
        /// <summary>
        /// The unique identifier for the Notification.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Some notifications are about content being posted in a parent, i.e. forums. This is the parent object name.
        /// </summary>
        public string ParentName { get; set; }

        /// <summary>
        /// The name of the content being referenced, i.e. the thread subject.
        /// </summary>
        public string ContentName { get; set; }

        /// <summary>
        /// The relative url to the content, i.e. the thread. This is where the notification should link through to.
        /// </summary>
        public string ContentUrl { get; set; }

        /// <summary>
        /// If the content is a photo, this returns the filestore id, i.e. {guid}.jpg
        /// </summary>
        public string ContentFilestoreId { get; set; }

        /// <summary>
        /// Signifies whether or not the content was posted by the user receiving the notification.
        /// </summary>
        public bool IsOwnContent { get; set; }

        /// <summary>
        /// If this notification relates directly to content, then this needs to be supplied, helping to tie the notification to the content and scenario intended.
        /// </summary>
        public NotificationType? ScenarioType { get; set; }

        /// <summary>
        /// Some notifications relate to people doing things, like replying to posts and some relate to multiple people such as a batch of unread replies.
        /// This will contain these users.
        /// </summary>
        public List<UserProfileLightDto> Users { get; set; }

        /// <summary>
        /// How many times a notification has occured for the notification subscription subject.
        /// When a notification has been read it is deleted and this counter essentially goes back to one for the next occurance of the notification.
        /// </summary>
        public int Occurances { get; set; }
        #endregion

        #region constructors
        public NotificationDto()
        {
            Occurances = 1;
            Users = new List<UserProfileLightDto>();
        }
        #endregion
    }
}