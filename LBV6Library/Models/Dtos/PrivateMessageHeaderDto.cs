using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LBV6Library.Models.Dtos
{
    public class PrivateMessageHeaderDto
    {
        #region accessors
        public long Id { get; set; }
        public DateTime? LastMessageCreated { get; set; }
        public List<PrivateMessageHeaderUserDto> Users { get; set; }
        public bool HasUnreadMessagesForCurrentUser { get; set; }
        #endregion

        #region constructors
        public PrivateMessageHeaderDto()
        {
            Users = new List<PrivateMessageHeaderUserDto>();
        }
        #endregion

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Users.Count < 2)
                yield return new ValidationResult("Private message headers must have at least two users.", new[] { "Users" });
        }
        #endregion
    }
}