using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("PrivateMessageHeaders")]
    public class PrivateMessageHeader
    {
        #region accessors
        /// <summary>
        /// The unique identifier for this header.
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The date the last message was created/sent in this header.
        /// </summary>
        public DateTime? LastMessageCreated { get; set; }
        /// <summary>
        /// The list of users who are authorised to send and recieve messages as part of the PM group.
        /// </summary>
        public List<PrivateMessageHeaderUser> Users { get; set; }
        #endregion

        #region constructors
        public PrivateMessageHeader()
        {
            Users = new List<PrivateMessageHeaderUser>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Users == null)
                yield return new ValidationResult("Users cannot be null.", new[] { "Users" });

            if (Users != null && Users.Count < 2)
                yield return new ValidationResult("At least two users are required.", new[] { "Users" });
        }
        #endregion
    }
}