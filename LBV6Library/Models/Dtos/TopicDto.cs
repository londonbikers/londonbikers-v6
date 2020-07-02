using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LBV6Library.Models.Dtos
{
    public class TopicDto
    {
        #region accessors
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public int UserMemberSince { get; set; }
        public string UserTagline { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public long ForumId { get; set; }
        public string ForumName { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }
        public bool IsSticky { get; set; }
        /// <summary>
        /// If this topic has been posted in a category that's marked as a gallery category then this will be set to true.
        /// </summary>
        public bool IsGallery { get; set; }
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public List<PostAttachmentDto> Attachments { get; set; }
        public List<PostModerationHistoryItemDto> ModerationHistoryItems { get; set; } 
        public byte StatusCode { get; set; }
        public DateTime? EditedOn { get; set; }
        public List<PhotoDto> Photos { get; set; }
        /// <summary>
        /// Provides a short-cut way of submitting a collection of photo ids with a reply creation.
        /// This simplifies the client experience as only one request needs to be made to finalise creation of the reply and 
        /// add the photos to the post.
        /// </summary>
        public List<Guid> PhotoIdsToIncludeOnCreation { get; set; }
        public bool ProtectPhotos { get; set; }

        /// <summary>
        /// Used just for creates to enable all photos to be tagged with a single credit.
        /// Requires going through and updating each photo at post creation time, but this seems
        /// the best in terms of UX as people may upload photos first, then add a comment.
        /// </summary>
        public string PhotoCredits { get; set; }
        #endregion

        #region constructors
        public TopicDto()
        {
            Created = DateTime.UtcNow;
            Photos = new List<PhotoDto>();
            PhotoIdsToIncludeOnCreation = new List<Guid>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ForumId < 1)
                yield return new ValidationResult("Topics require a forum id.", new[] { "ForumId" });

            if (string.IsNullOrEmpty(UserId) || string.IsNullOrWhiteSpace(UserId))
                yield return new ValidationResult("Topics must have a valid user id.", new[] { "UserId" });

            if (string.IsNullOrEmpty(Subject) || string.IsNullOrWhiteSpace(Subject))
                yield return new ValidationResult("Topics must have a valid subject.", new[] { "Subject" });

            if (Id < 1 && (string.IsNullOrEmpty(Content) || string.IsNullOrWhiteSpace(Content)) && PhotoIdsToIncludeOnCreation.Count == 0)
                yield return new ValidationResult("New Topics with no content must be supplied with PhotoIdsToIncludeOnCreation entries.", new[] { "Content" });

            if (Id < 1 && (string.IsNullOrEmpty(Content) || string.IsNullOrWhiteSpace(Content)) && Photos.Count == 0)
                yield return new ValidationResult("Existing Topics with no content must be supplied with photo ids.", new[] { "Content" });

            if (string.IsNullOrEmpty(UserId))
                yield return new ValidationResult("Topics require an author id.", new[] { "UserId" });
        }
        #endregion
    }
}