using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("PrivateMessageUsers")]
    public class PrivateMessageHeaderUser
    {
        public long Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public DateTime Added { get; set; }
        [Column("PrivateMessageHeader_Id")]
        public long PrivateMessageHeaderId { get; set; }
        public bool HasUnreadMessages { get; set; }
        public bool? HasDeleted { get; set; }

        public PrivateMessageHeaderUser()
        {
            Added = DateTime.UtcNow;
        }

        public PrivateMessageHeaderUser(string userId)
        {
            UserId = userId;
            Added = DateTime.UtcNow;
        }
    }
}