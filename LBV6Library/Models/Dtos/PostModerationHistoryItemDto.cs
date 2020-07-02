using System;

namespace LBV6Library.Models.Dtos
{
    /// <summary>
    /// Represents an action taken on a post by a moderator.
    /// </summary>
    public class PostModerationHistoryItemDto
    {
        public string ModeratorId { get; set; }
        public string ModeratorUserName { get; set; }
        public string Type { get; set; }
        public string Reason { get; set; }
        public DateTime Created { get; set; }
    }
}