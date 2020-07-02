using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents a small amount of publicly available information for a user.
    /// </summary>
    public class UserProfileExtendedDto : UserProfileDto
    {
        // privileged attributes
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public Gender? Gender { get; set; }
        public string Occupation { get; set; }
        public List<string> Logins { get; set; }
        
        #region constructors
        public UserProfileExtendedDto()
        {
            Logins = new List<string>();
        }
        #endregion
    }
}