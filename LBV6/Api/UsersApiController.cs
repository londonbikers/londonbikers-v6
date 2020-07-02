using ImageResizer;
using ImageResizer.ExtensionMethods;
using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LBV6.Api
{
    [Authorize]
    public class UsersApiController : ApiController
    {
        #region public methods
        [HttpGet]
        public async Task<IHttpActionResult> GetUser(string userId)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (user == null)
                return NotFound();

            var dto = Transformations.ConvertUserToUserProfileLightDto(user);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IHttpActionResult> ChangeProfilePhoto()
        {
            if (HttpContext.Current.Request.Files.Count > 1)
                return BadRequest("Only a single file can be uploaded.");

            var file = HttpContext.Current.Request.Files[0];
            if (file.ContentLength <= 0)
                return BadRequest("No content provided.");

            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());

            // assuming all files have extensions, but what if they don't, i.e. images from macs. log this
            if (!file.FileName.Contains('.'))
            {
                Logging.LogWarning(GetType().FullName, "No extension found on file: " + file.FileName);
                return BadRequest("No extension found on file.");
            }

            // if the image isn't a JPEG, convert it to one
            var memoryStream = new MemoryStream();
            if (!Path.GetExtension(file.FileName).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase))
                ImageBuilder.Current.Build(file.InputStream, memoryStream, new ResizeSettings("format=jpg"));
            else
                memoryStream = file.InputStream.CopyToMemoryStream(true);

            await ForumServer.Instance.Photos.StoreProfilePhotoAsync(memoryStream, user);

            // return the basic profile photo URL
            var photoUrl = Utilities.GetFileStoreIdForProfilePhoto(user);
            return Ok(photoUrl);
        }

        /// <summary>
        /// Allows the current user to amend some of their profile attributes.
        /// </summary>
        [HttpPost]
        public async Task<IHttpActionResult> UpdateProfile(UserProfileDto profileDto)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            user.Biography = profileDto.Biography;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
            return Ok();
        }

        [HttpPost]
        [CheckModelForNull]
        public async Task<IHttpActionResult> UpdatePreferences(UserProfileDto.UserPreferences preferences)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            user.ReceiveNewsletters = preferences.ReceiveNewsletters;
            user.NewTopicNotifications = preferences.NewTopicNotifications;
            user.NewReplyNotifications = preferences.NewReplyNotifications;
            user.NewPhotoCommentNotifications = preferences.NewPhotoCommentNotifications;
            user.NewMessageNotifications = preferences.NewMessageNotifications;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
            return Ok();
        }

        [HttpGet]
        public async Task<IHttpActionResult> SearchUsers(string term, int maxResults)
        {
            var ids = await ForumServer.Instance.Users.SearchUsersByUsernameAsync(term, maxResults);
            if (ids == null)
                return NotFound();

            var users = new List<User>();
            foreach (var id in ids)
                users.Add(await ForumServer.Instance.Users.GetUserAsync(id));

            var dtos = Helpers.ConvertUsersToUserProfileLightDtos(users);
            return Ok(dtos);
        }
        #endregion

        #region admin methods
        // GET: api/users/SearchUsersByUsernameOrEmail?term=x&limit=y&startindex=z
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> SearchUsersByUsernameOrEmail(string term, int limit, int startIndex)
        {
            if (string.IsNullOrEmpty(term))
                return Redirect(new Uri("GetLatestUsers?limit=" + limit + "&startIndex=" + startIndex, UriKind.Relative));

            // hard limit the number of users that can be returned
            if (limit > 50)
                limit = 50;

            var usersContainer =
                await ForumServer.Instance.Users.SearchUsersByUsernameOrEmailAsync(term, limit, startIndex);
            if (usersContainer == null)
                return NotFound();

            // ensure we have extended information for the users
            foreach (var user in usersContainer.Users.Where(q => !q.TopicsCount.HasValue))
                await ForumServer.Instance.Users.GetUserExtendedInformationAsync(user);

            var userDtos = Transformations.ConvertUsersToUserProfileLightExtendedDtos(usersContainer.Users);
            var response = new { Users = userDtos, TotalItems = usersContainer.TotalUsers };
            return Ok(response);
        }

        // GET: api/users/GetLatestUsers?limit=x&startindex=y
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> GetLatestUsers(int limit, int startIndex)
        {
            // hard limit the number of users that can be returned
            if (limit > 50)
                limit = 50;

            var usersContainer = await ForumServer.Instance.Users.GetLatestUsersAsync(limit, startIndex);
            if (usersContainer == null)
                return NotFound();

            // ensure we have extended information for the users
            foreach (var user in usersContainer.Users.Where(q => !q.TopicsCount.HasValue))
                await ForumServer.Instance.Users.GetUserExtendedInformationAsync(user);

            var userDtos = Transformations.ConvertUsersToUserProfileLightExtendedDtos(usersContainer.Users);
            var response = new { Users = userDtos, TotalItems = usersContainer.TotalUsers };
            return Ok(response);
        }

        // GET: api/users/GetUserStats
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> GetUserStats()
        {
            var stats = await ForumServer.Instance.Users.GetUserStatsAsync();
            return Ok(stats);
        }

        // POST: api/users/RemoveProfileCover?userId=xxx
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> RemoveProfileCover(string userId)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user's profile cover.");

            await ForumServer.Instance.Photos.DeleteProfileCoverPhotoAsync(user);
            return Ok();
        }

        // POST: api/users/RemoveProfilePhoto?userId=xxx
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> RemoveProfilePhoto(string userId)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (!user.ProfilePhotoVersion.HasValue)
                return Ok();

            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user's profile photo.");

            await ForumServer.Instance.Photos.DeleteProfilePhotoAsync(user);
            return Ok();
        }

        // POST: api/users/RemoveUserLogin?userId=xxx&providerName=yyy
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> RemoveLogin(string userId, string providerName)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user' logins.");

            await ForumServer.Instance.Users.RemoveUserLoginAsnc(userId, providerName);
            return Ok();
        }

        // POST: api/users/UpdateUsername?userId=xxx&username=yyy
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> UpdateUsername(string userId, string username)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that users username.");

            switch (await ForumServer.Instance.Users.UpdateUserNameAsync(userId, username))
            {
                case UserNameValidationResult.Valid:
                    return Ok();
                case UserNameValidationResult.InvalidCharacters:
                    return BadRequest("Username contains invalid characters.");
                case UserNameValidationResult.InvalidTooLong:
                    return BadRequest("Username cannot be longer than " + ConfigurationManager.AppSettings["LB.Usernames.MaxLength"] + " characters.");
                case UserNameValidationResult.InvalidTooShort:
                    return BadRequest("Username has to be "+ ConfigurationManager.AppSettings["LB.Usernames.MinLength"] + " or more characters long.");
                case UserNameValidationResult.InvalidAlreadyInUse:
                    return BadRequest("Username is already in use.");
                case UserNameValidationResult.InvalidReservedWord:
                    return BadRequest("Username contains prohibited words");
                default:
                    return BadRequest("Responses need updating.");
            }
        }

        // POST: api/users/UpdateEmailAddress?userId=xxx&emailAddress=yyy
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> UpdateEmailAddress(string userId, string emailAddress)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that users email address.");

            var result = await ForumServer.Instance.Users.UpdateEmailAddressAsync(userId, emailAddress);
            if (result)
                return Ok();

            return BadRequest("Email address is already in use by another account.");
        }

        // POST: api/users/UpdateVerifiedState?userId=xxx&verified=true|false
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> UpdateVerifiedState(string userId, bool verified)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user's verified state.");

            user.Verified = verified;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
            return Ok();
        }

        // POST: api/users/UpdateStatus?userId=xxx&status=0|1|2
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> UpdateStatus(string userId, int status)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user's status.");

            user.Status = (UserStatus)status;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
            return Ok();
        }

        // POST: api/users/UpdateProfileExtended
        /// <summary>
        /// Updates select extended attributes for a user.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public async Task<IHttpActionResult> UpdateProfileExtended(UserProfileExtendedDto profile)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(profile.Id);
            if (Helpers.IsIllegalSuperUserOperation(user))
                return BadRequest("Cannot change that user's profile information.");

            user.Biography = profile.Biography;
            user.FirstName = profile.FirstName;
            user.LastName = profile.LastName;
            user.Occupation = profile.Occupation;
            user.Tagline = profile.Tagline;

            await ForumServer.Instance.Users.UpdateUserAsync(user);
            return Ok();
        }
        #endregion
    }
}