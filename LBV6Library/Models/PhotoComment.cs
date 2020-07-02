using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("PhotoComments")]
    public class PhotoComment : INotCachable, IComparable<PhotoComment>
    {
        #region accessors
        /// <summary>
        /// The unique identifier for the photo comment.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// PhotoComments can be in reply to another, i.e. they can be nested.
        /// </summary>
        public long? ParentCommentId { get; set; }

        /// <summary>
        /// The unique identifier for the photo this comment is against. This must always be populated even when the comment is nested.
        /// </summary>
        public Guid PhotoId { get; set; }

        /// <summary>
        /// The unique identifier for the user who posted this comment.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// The date the comment was created.
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        /// The actual comment text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Photo comments made in reply to this one.
        /// </summary>
        [ForeignKey("ParentCommentId")]
        public List<PhotoComment> ChildComments { get; set; }
        #endregion

        #region legacy accessors
        public long? LegacyCommentId { get; set; }
        #endregion

        #region constructors
        public PhotoComment() 
        {
            ChildComments = new List<PhotoComment>();
        }
        #endregion

        #region methods
        /// <summary>
        /// Verifies whether or not the PhotoComment is valid for persistence.
        /// </summary>
        public bool IsValid()
        {
            if (PhotoId == Guid.Empty)
                return false;

            if (string.IsNullOrEmpty(UserId))
                return false;

            if (string.IsNullOrEmpty(Text))
                return false;

            return true;
        }
        #endregion

        #region IComparable methods
        public int CompareTo(PhotoComment other)
        {
            // photo comments need to be sorted by date
            return other == null ? 1 : Created.CompareTo(other.Created);
        }
        #endregion
    }
}