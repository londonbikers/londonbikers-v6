using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LBV6Library.Models.Dtos
{
    public class ReplyDto
    {
        #region accessors
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public long ParentPostId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public int UserMemberSince { get; set; }
        public int? UserProfilePhotoVersion { get; set; }
        public string UserTagline { get; set; }
        public string Content { get; set; }
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public List<PostAttachmentDto> Attachments { get; set; }
        public List<PostModerationHistoryItemDto> ModerationHistoryItems { get; set; } 
        public byte StatusCode { get; set; }
        public bool SubscribeToTopic { get; set; }
        public DateTime? EditedOn { get; set; }
        public List<PhotoDto> Photos { get; set; }
        /// <summary>
        /// Provides a short-cut way of submitting a collection of photo ids with a reply creation.
        /// This simplifies the client experience as only one request needs to be made to finalise creation of the reply and 
        /// add the photos to the post.
        /// </summary>
        public List<Guid> PhotoIdsToIncludeOnCreation { get; set; }
        #endregion

        #region constructors
        public ReplyDto()
        {
            Created = DateTime.UtcNow;
            Photos = new List<PhotoDto>();
            PhotoIdsToIncludeOnCreation = new List<Guid>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (ParentPostId < 1)
                yield return new ValidationResult("Replies must have a parent post id.", new[] { "ParentPostId" });

            if (string.IsNullOrEmpty(UserId) || string.IsNullOrWhiteSpace(UserId))
                yield return new ValidationResult("Replies must have a valid user id.", new[] { "UserId" });

            if (Id < 1 && (string.IsNullOrEmpty(Content) || string.IsNullOrWhiteSpace(Content)) && PhotoIdsToIncludeOnCreation.Count == 0)
                yield return new ValidationResult("New replies with no content must be supplied with PhotoIdsToIncludeOnCreation entries.", new[] { "Content" });

            if (Id < 1 && (string.IsNullOrEmpty(Content) || string.IsNullOrWhiteSpace(Content)) && Photos.Count == 0)
                yield return new ValidationResult("Existing replies with no content must be supplied with photo ids.", new[] { "Content" });

            if (string.IsNullOrEmpty(UserId))
                yield return new ValidationResult("Topics and replies require an author id.", new[] { "UserId" });
        }
        #endregion
    }
}