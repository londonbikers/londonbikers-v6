namespace LBV6Library.Models
{
    /// <summary>
    /// Identifies the status of a post.
    /// </summary>
    public enum PostStatus
    {
        /// <summary>
        /// An active post should be shown.
        /// </summary>
        Active = 0,
        /// <summary>
        /// A remove post should not be visible, except to moderators in a dedicated view.
        /// </summary>
        Removed = 1,
        /// <summary>
        /// A closed post cannot accept any more replies.
        /// </summary>
        Closed = 2
    }

    /// <summary>
    /// Identifies the type of moderation taken against a post.
    /// </summary>
    public enum ModerationType
    {
        /// <summary>
        /// Marks a post that has been moved to another forum.
        /// </summary>
        Moved = 0,
        /// <summary>
        /// Marks a post that has been closed and cannot accept any more replies.
        /// </summary>
        Closed = 1,
        /// <summary>
        /// Marks a post that has had it's content edited by a moderator.
        /// </summary>
        Edited = 2,
        /// <summary>
        /// Marks a post that has been removed and is not visible.
        /// </summary>
        Removed = 3
    }

    public enum EmailTemplate
    {
        EmailConfirmationRequired,
        NewPrivateMessage,
        NewPrivateMessageWithRollups,
        NewTopic,
        NewTopicReply,
        NewTopicReplyWithPhoto,
        NewTopicReplyWithPhotoWithRollups,
        NewTopicReplyWithPhotoWithoutContent,
        NewTopicReplyWithPhotoWithoutContentWithRollups,
        NewTopicReplyWithPhotos,
        NewTopicReplyWithPhotosWithRollups,
        NewTopicReplyWithPhotosWithoutContent,
        NewTopicReplyWithPhotosWithoutContentWithRollups,
        NewTopicReplyWithRollups,
        NewTopicWithPhoto,
        NewTopicWithPhotoWithRollups,
        NewTopicWithPhotoWithoutContent,
        NewTopicWithPhotoWithoutContentWithRollups,
        NewTopicWithPhotos,
        NewTopicWithPhotosWithRollups,
        NewTopicWithPhotosWithoutContent,
        NewTopicWithPhotosWithoutContentWithRollups,
        NewTopicWithRollups,
        PostModerationNotification,
        RegistrationEmailConfirmationRequired,
        ResetPasswordLink,
        Welcome,
        NewPhotoComment,
        NewPhotoCommentWithRollups
    }

    public enum Gender
    {
        Male = 0,
        Female = 1
    }

    /// <summary>
    /// Defines what type a notification is
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// A notification for when someone posts a topic in a forum that's being followed.
        /// </summary>
        NewTopic = 1,
        /// <summary>
        /// A notification for when a new reply is posted against a topic someone is following.
        /// </summary>
        NewTopicReply = 2,
        /// <summary>
        /// A notification for when a users topic is moderated.
        /// </summary>
        TopicModeration = 3,
        /// <summary>
        /// A notification for when a users reply is moderated.
        /// </summary>
        ReplyModeration = 4,
        /// <summary>
        /// A notification for when a user receives a private message.
        /// </summary>
        NewPrivateMessage = 5,
        /// <summary>
        /// A notification for when a someone posts a comment on a photo of yours or a photo you've commented on.
        /// </summary>
        NewPhotoComment = 6
    }

    public enum UserStatus
    {
        Active = 0,
        Suspended = 1,
        Banned = 2
    }

    public enum UserNameValidationResult
    {
        Valid = 0,
        InvalidTooShort = 1,
        InvalidTooLong = 2,
        InvalidCharacters = 3,
        InvalidReservedWord = 4,
        InvalidAlreadyInUse = 5
    }

    /// <summary>
    /// Defines what type of message a private message is. Messages can be just messages, i.e. written by
    /// users or they can be system messages that are conveying an event. As such these events can be shown
    /// in the conversation timeline.
    /// </summary>
    public enum PrivateMessageType
    {
        Message = 0,
        UserAdded = 1,
        UserLeft = 2,
        UserRemoved = 3
    }

    /// <summary>
    /// Indicates how a user was removed from a header, i.e. by themselves or someone else did it.
    /// </summary>
    public enum PrivateMessageUserOperationType
    {
        UserRemovedSelf,
        UserWasRemoved
    }

    /// <summary>
    /// Post indexes are stored in a hash-list with the role combinations in serialised form being the key
    /// names. Some are fixed though and this enumeration helps reduce the use of strings in referencing known key names
    /// which of course could be prone to developer typos.
    /// </summary>
    public enum PostIndexType
    {
        Anonymous
    }

    public enum AccessControlRole
    {
        Administrator,
        Moderator
    }
}