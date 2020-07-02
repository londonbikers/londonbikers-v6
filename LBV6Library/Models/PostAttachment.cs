using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LBV6Library.Interfaces;

namespace LBV6Library.Models
{
    public class PostAttachment : ICommon, INotCachable
    {
        public long Id { get; set; }
        [Column("Post_Id")]
        public long PostId { get; set; }
        public DateTime Created { get; set; }
        [Required]
        public string Filename { get; set; }
        [Required]
        public string ContentType { get; set; }
        public long Views { get; set; }
        public int? LegacyId { get; set; }
    }
}