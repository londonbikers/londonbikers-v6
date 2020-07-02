using System;
using System.ComponentModel.DataAnnotations;

namespace LBV6Library.Models
{
    /// <summary>
    /// Represents an occurance of a user viewing a topic. Used to help derive stats for things like popular posts.
    /// </summary>
    public class TopicView
    {
        /// <summary>
        /// Used for EF to identify the record.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The unique identifier for the topic being viewed.
        /// </summary>
        [Required]
        public long TopicId { get; set; }
        /// <summary>
        /// The user-id (guid) of the user who viewed the topic. Leave as null for anonymous user views.
        /// </summary>
        public string UserId { get; set; }
        /// <summary>
        /// The ip-address of the user who viewed the topic. Only supply for anonymous users and don't supply for authenticated users.
        /// </summary>
        public string Ip { get; set; }
        /// <summary>
        /// The time when the user viewed the topic.
        /// </summary>
        [Required]
        public DateTime When { get; set; }

        #region constructors
        public TopicView()
        {
            When = DateTime.UtcNow;
        }
        #endregion
    }
}
