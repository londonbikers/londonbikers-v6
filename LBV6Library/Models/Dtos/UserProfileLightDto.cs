using System;

namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents the minimum amount of publicly available information for a user. 
    /// Useful for search results for example.
    /// </summary>
    public class UserProfileLightDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public DateTime Joined { get; set; }
        public UserStatus Status { get; set; }
        public string Tagline { get; set; }
        public string ProfileFileStoreId { get; set; }
        public bool? Verified { get; set; }
    }
}