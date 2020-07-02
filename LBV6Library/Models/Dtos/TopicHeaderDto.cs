using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class TopicHeaderDto
    {
        public long Id { get; set; }
        public DateTime Updated { get; set; }
        public long ForumId { get; set; }
        public string ForumName { get; set; }
        public string Subject { get; set; }
        public bool IsSticky { get; set; }
        public int UpVotes { get; set; }
        public int DownVotes { get; set; }
        public int ReplyCount { get; set; }
        public byte StatusCode { get; set; }

        public bool? IsNew { get; set; }
        public bool? HasNewReplies { get; set; }
        public long? FirstUnreadReplyId { get; set; }
        public int? FirstUnreadReplyPosition { get; set; }

        public bool HasAttachments { get; set; }
        public bool HasPhotos { get; set; }

        /// <summary>
        /// Contains a collection of user link DTOs representing users that feature prominently in the thread.
        /// </summary>
        public List<ProminentUserDto> ProminentUsers { get; set; }

        #region  constructors
        public TopicHeaderDto()
        {
            ProminentUsers = new List<ProminentUserDto>();
        }
        #endregion
    }
}