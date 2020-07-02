using LBV6Library.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    /// <summary>
    /// Defines a role that determines who can and cannot post in a particular forum.
    /// </summary>
    public class ForumPostRole : INotCachable
    {
        public int Id { get; set; }
        [Column("Forum_Id")]
        public long ForumId { get; set; }
        public string Role { get; set; }
    }
}