using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("AspNetUserClaims")]
    public class UserClaim
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        public string ClaimType { get; set; }
        public string ClaimValue { get; set; }
    }
}
