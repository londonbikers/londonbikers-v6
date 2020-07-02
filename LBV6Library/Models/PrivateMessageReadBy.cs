using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("PrivateMessageReadBys")]
    public class PrivateMessageReadBy
    {
        public long Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public DateTime When { get; set; }
        [Column("PrivateMessage_Id")]
        public long PrivateMessageId { get; set; }

        public PrivateMessageReadBy()
        {
            When = DateTime.UtcNow;
        }
    }
}