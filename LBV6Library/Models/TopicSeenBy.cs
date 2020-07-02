using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("TopicSeenBys")]
    public class TopicSeenBy
    {
        [Key, Column(Order = 0)]
        public long PostId { get; set; }
        [Key, Column(Order = 1)]
        public string UserId { get; set; }
        public long LastPostIdSeen { get; set; }
    }
}