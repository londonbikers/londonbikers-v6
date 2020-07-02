using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("Forums")]
    public class Forum : ICommon, INotCachable
    {
        #region persisted accessors
        public long Id { get; set; }
        public DateTime Created { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// If populated, a user must be authenticated and must have one of the roles in this list to be able to access the forum.
        /// </summary>
        public List<ForumAccessRole> AccessRoles { get; set; }
        /// <summary>
        /// If populated, a user must have one of the roles contained in this collection to be able to post in the forum.
        /// </summary>
        public List<ForumPostRole> PostRoles { get; set; }
        /// <summary>
        /// The number of posts (topics and replies combined) within the forum.
        /// </summary>
        public long PostCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        [Column("Category_Id")]
        public long CategoryId { get; set; }
        /// <summary>
        /// If set to true, UIs should attempt to stop the user from downloading topic photos and provide a link to where the photos can be bought.
        /// </summary>
        public bool ProtectTopicPhotos { get; set; }
        #endregion

        #region transient accessors
        /// <summary>
        /// Indexes a number of the most recent active threads in this forum. Saves querying the database for every request.
        /// For results outside of this index the database will have to be queried though seeing as mosts request are for new
        /// posts this should not have to be done too often. 
        /// Key is post id, value is whether or not the post is pinned (used for ordering purposes).
        /// </summary>
        [NotMapped]
        public List<KeyValuePair<long, bool>> RecentTopicsIndex { get; set; }
        #endregion

        #region constructors
        public Forum()
        {
            Created = DateTime.UtcNow;
            PostRoles = new List<ForumPostRole>();
            AccessRoles = new List<ForumAccessRole>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name))
                yield return new ValidationResult("Forum requires a name.", new[] {"Name"});

            if (CategoryId < 1)
                yield return new ValidationResult("Forum requires a parent category id.", new[] {"Category"});
        }
        #endregion

        #region overrides
        public override string ToString()
        {
            return $"{Id} - {Name}";
        }
        #endregion
    }
}