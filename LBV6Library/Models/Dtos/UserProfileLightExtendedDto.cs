using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents the minimum amount of internally available information for a user. 
    /// Useful for search results for example.
    /// </summary>
    public class UserProfileLightExtendedDto : UserProfileLightDto
    {
        // privileged attributes
        public string Email { get; set; }
        public DateTime Created { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Logins { get; set; }
        public int TopicsCount { get; set; }
        public int RepliesCount { get; set; }
        public bool EmailConfirmed { get; set; }

        #region constructors
        public UserProfileLightExtendedDto()
        {
            Logins = new List<string>();
        }
        #endregion
    }
}