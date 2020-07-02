using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace LBV6Library.Models
{
    [Table("Posts")]
    public class Post : ICommon, ICachable
    {
        #region primary accessors
        public long Id { get; set; }
        [Required]
        public DateTime Created { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public bool? IsSticky { get; set; }
        public int? UpVotes { get; set; }
        public int? DownVotes { get; set; }
        public bool? IsAnswer { get; set; }
        public long? Views { get; set; }
        public DateTime? EditedOn { get; set; }
        [Required]
        [Column("User_Id")]
        public string UserId { get; set; }
        [Column("Forum_Id")]
        public long? ForumId { get; set; }
        /// <summary>
        /// The Parent Post id if this is a reply.
        /// </summary>
        [Column("ParentPost_Id")]
        public long? PostId { get; set; }
        /// <summary>
        /// The date of the last reply for a topic.
        /// Persisted to the database to enable quick ordering of topics within a forum, showing those with replies before those without any.
        /// </summary>
        public DateTime? LastReplyCreated { get; set; }
        public virtual List<Post> Replies { get; set; }
        [Required]
        public PostStatus Status { get; set; }
        public List<PostModerationHistoryItem> ModerationHistory { get; set; }
        public List<Photo> Photos { get; set; }
        #endregion

        #region legacy accessors
        public int? LegacyPostId { get; set; }
        public long? LegacyGalleryId { get; set; }
        public long? LegacyCommentId { get; set; }
        /// <summary>
        /// Legacy accessor for old forum attachments. Do not use for new posts.
        /// </summary>
        public List<PostAttachment> Attachments { get; set; }
        #endregion

        #region transient accessors
        /// <summary>
        /// Used when creating replies to indicate whether or not the user should be subscribed to the post as well.
        /// Eventually this should be used for reading as well by the UI once reading notification status is fast enough.
        /// </summary>
        [NotMapped]
        public bool SubscribeToTopic { get; set; }

        [NotMapped]
        public bool IsTopic => ForumId.HasValue && !PostId.HasValue;
        
        /// <summary>
        /// Provides a short-cut way of submitting a collection of photo ids with a post creation.
        /// This simplifies the client experience as only one request needs to be made to finalise creation and add the photos to the post.
        /// </summary>
        [NotMapped]
        public List<Guid> PhotoIdsToIncludeOnCreation { get; set; }

        /// <summary>
        /// The total number of replies that are available to the public, i.e. not including ones that have been marked as removed.
        /// </summary>
        [NotMapped]
        public int ReplyCountPublic
        {
            get { return Replies.Count(q => q.Status != PostStatus.Removed); }
        }
        #endregion

        #region constructors
        public Post()
        {
            Created = DateTime.UtcNow;
            Status = PostStatus.Active;
            ModerationHistory = new List<PostModerationHistoryItem>();
            Replies = new List<Post>();
            Photos = new List<Photo>();
            PhotoIdsToIncludeOnCreation = new List<Guid>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!PostId.HasValue && string.IsNullOrEmpty(Subject))
                yield return new ValidationResult("Topics require a subject.", new[] { "Subject" });

            if (!PostId.HasValue && string.IsNullOrWhiteSpace(Subject))
                yield return new ValidationResult("Topics require a subject with more than whitespace in.", new[] { "Subject" });

            if (!PostId.HasValue && !ForumId.HasValue)
                yield return new ValidationResult("Topics require a forum id.", new[] { "ForumId" });

            if (!PostId.HasValue && IsAnswer.HasValue)
                yield return new ValidationResult("Topics cannot be an answer.", new[] { "IsAnswer" });

            if (PostId.HasValue && IsSticky.HasValue)
                yield return new ValidationResult("Replies cannot be sticky.", new[] { "IsSticky" });

            if (PostId.HasValue && !string.IsNullOrEmpty(Subject) && !string.IsNullOrWhiteSpace(Subject))
                yield return new ValidationResult("Replies cannot have a subject.", new[] { "Subject" });

            if (string.IsNullOrEmpty(UserId))
                yield return new ValidationResult("Topics and replies require an author id.", new[] { "UserId" });
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return !PostId.HasValue ? $"Topic {Id} - {Subject}" : $"Reply {Id} - {Utilities.GetContentSynopsis(Content)}";
        }
        #endregion

        #region caching
        [NotMapped]
        public string CacheKey => $"{typeof (Post).Name}-{Id}";

        // if only c# allowed static properties on interfaces, this would be simpler
        public static string GetCacheKey(long id)
        {
            return $"{typeof (Post).Name}-{id}";
        }
        #endregion
    }
}