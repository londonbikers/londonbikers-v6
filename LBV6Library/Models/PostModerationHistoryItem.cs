using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    /// <summary>
    /// Represents an action taken on a post by a moderator.
    /// </summary>
    public class PostModerationHistoryItem
    {
        public long Id { get; set; }
        [Required]
        [Column("Post_Id")]
        public long PostId { get; set; }
        [Required]
        [Column("Moderator_Id")]
        public string ModeratorId { get; set; }
        [Required]
        public ModerationType Type { get; set; }
        [Required]
        public string Justification { get; set; }
        [Required]
        public DateTime Created { get; set; }

        #region constructors
        public PostModerationHistoryItem()
        {
            Created = DateTime.UtcNow;
        }
        #endregion
    }
}