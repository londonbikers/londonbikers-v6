using LBV6Library.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("AspNetUserLogins")]
    public class UserLogin : INotCachable
    {
        [Key, Column(Order=0)]
        public string LoginProvider { get; set; }
        [Key, Column(Order = 1)]
        public string ProviderKey { get; set; }
        [Key, Column(Order = 2)]
        public string UserId { get; set; }
    }
}