using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("PrivateMessages")]
    public class PrivateMessage
    {
        public long Id { get; set; }
        [Required]
        [Column("PrivateMessageHeader_Id")]
        public long PrivateMessageHeaderId { get; set; }
        [Required]
        public string UserId { get; set; }
        public DateTime Created { get; set; }
        public string Content { get; set; }
        public List<PrivateMessageReadBy> ReadBy { get; set; }
        public int? LegacyMessageId { get; set; }
        public List<PrivateMessageAttachment> Attachments { get; set; }
        public PrivateMessageType Type { get; set; }

        #region constructors
        public PrivateMessage()
        {
            ReadBy = new List<PrivateMessageReadBy>();
            Created = DateTime.UtcNow;
            Type = PrivateMessageType.Message;
        }
        #endregion

        #region public methods
        public override string ToString()
        {
            return $"Private Message {Id} - {Utilities.GetContentSynopsis(Content)}";
        }
        #endregion
    }
}