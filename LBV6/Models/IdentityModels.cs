using LBV6Library.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LBV6.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    /// <summary>
    /// Model of the user for use in the web-app only. For use with ASPNET Identity.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public DateTime Created { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool ReceiveNewsletters { get; set; }

        // Facebook specific data
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public Gender? Gender { get; set; }

        public Guid? LegacyApolloId { get; set; }
        public int? LegacyForumId { get; set; }
        public int TotalVists { get; set; }
        public UserStatus Status { get; set; }
        public DateTime? DateOfBirth { get; set; }
        /// <summary>
        /// A user's description about themselves.
        /// </summary>
        public string Biography { get; set; }
        public string Occupation { get; set; }
        /// <summary>
        /// The piece of text displayed next to a person's name, i.e. 'Chief Whip'.
        /// </summary>
        public string Tagline { get; set; }
        public bool WelcomeEmailSent { get; set; }

        #region constructors
        public ApplicationUser()
        {
            ReceiveNewsletters = true;
            Status = UserStatus.Active;
            Created = DateTime.UtcNow;
        }
        #endregion

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);

            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("DefaultConnection", false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}