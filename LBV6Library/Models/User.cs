using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    /// <inheritdoc />
    /// <summary>
    /// A compact mirror of the ASPNET Identity model for reference purposes.
    /// Most management of users will still be done via ASP.NET Identity, excluding:
    /// - Preferences
    /// </summary>
    [Table("AspNetUsers")]
    public class User : ICachable
    {
        #region constructors
        public User()
        {
            Logins = new List<string>();
        }
        #endregion

        #region basic information
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Created { get; set; }
        public int? AgeMin { get; set; }
        public int? AgeMax { get; set; }
        public Gender? Gender { get; set; }
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
        /// <summary>
        /// When this has a value it represents a part of the user profile photo file name in the file store.
        /// The location of the file can be determined by combining the users creation date with their id and this version number.
        /// This keeps the data model as light as possible.
        /// </summary>
        public int? ProfilePhotoVersion { get; set; }
        /// <summary>
        /// Indicates whether or not an account is verified, i.e. the user is who they purport to be. This is not to be confused with confirmation
        /// where authentication details are confirmed. Verified user accounts are especially useful for business' we may wish to work with to give confidence
        /// to users that this is an official account for that person/company.
        /// </summary>
        public bool? Verified { get; set; }
        #endregion

        #region cover photo
        /// <summary>
        /// If the user has a cover photo for their profile, this will contain the object-store id for the file.
        /// </summary>
        public Guid? CoverPhotoId { get; set; }
        /// <summary>
        /// If the user has a cover photo for their profile, this will represent the width of the image in pixels.
        /// </summary>
        public int? CoverPhotoWidth { get; set; }
        /// <summary>
        /// If the user has a cover photo for their profile, this will represent the height of the image in pixels.
        /// </summary>
        public int? CoverPhotoHeight { get; set; }
        /// <summary>
        /// If the user has a cover photo for their profile, this will contain the content-type for the file, i.e. 'image/jpeg'
        /// This will be used in constructing a url with the file extension on.
        /// </summary>
        public string CoverPhotoContentType { get; set; }
        #endregion

        #region tracking
        /// <summary>
        /// The total number of sessions for the user.
        /// </summary>
        public int TotalVists { get; set; }
        /// <summary>
        /// The last time the user visited the site.
        /// </summary>
        public DateTime? LastVisit { get; set; }
        #endregion

        #region extended information
        // this information is lazy loaded on demand by the user controller as so not to impact initial retrieval of user 
        // information which is normally done when querying for indexes where multiple users are retrieved at once and 
        // where any increase in load times would multiply in effect.
        [NotMapped]
        public int? TopicsCount { get; set; }
        [NotMapped]
        public int? RepliesCount { get; set; }
        [NotMapped]
        public int? PhotosCount { get; set; }
        [NotMapped]
        public int? ModerationsCount { get; set; }
        /// <summary>
        /// The name of external logins associated with this users account.
        /// </summary>
        [NotMapped]
        public List<string> Logins { get; set; }
        #endregion

        #region stats
        /// <summary>
        /// The number of Intercom messages has that they haven't read yet.
        /// This is privileged information.
        /// </summary>
        public int UnreadMessagesCount { get; set; }
        /// <summary>
        /// The number of notifications the user has that have not been invalidated yet (i.e. seen) and do not pertain to new message notifications.
        /// This is privileged information.
        /// </summary>
        public int NonMessageNotificationsCount { get; set; }
        #endregion

        #region preferences
        public bool NewTopicNotifications { get; set; }
        public bool NewReplyNotifications { get; set; }
        public bool NewPhotoCommentNotifications { get; set; }
        public bool NewMessageNotifications { get; set; }
        public bool ReceiveNewsletters { get; set; }
        #endregion

        #region caching
        [NotMapped]
        public string CacheKey => $"{typeof (User).Name}-{Id}";

        // if only c# allowed static properties on interfaces, this would be simpler
        public static string GetCacheKey(string id)
        {
            return $"{typeof (User).Name}-{id}";
        }
        #endregion
    }
}