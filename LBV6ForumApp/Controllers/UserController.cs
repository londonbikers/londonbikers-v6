using Facebook;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Containers;
using LBV6Library.Models.Google;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    public class UserController
    {
        #region constructors
        internal UserController(PostController postController, PhotosController filesController)
        {
            postController.TopicCreated += ProcessTopicCreated;
            postController.ReplyCreated += ProcessReplyCreated;
            postController.ReplyRemoved += ProcessReplyRemoved;
            postController.TopicMoved += ProcessTopicMoved;
            postController.TopicRemoved += ProcessTopicRemoved;
            filesController.PhotoAddedToPost += ProcessPhotoAddedToPost;
        }
        #endregion

        #region events
        public delegate void UserEventHandler(User user);

        public event UserEventHandler UserCreated;
        protected virtual void OnUserCreated(EventArgs e, User user)
        {
            var handler = UserCreated;
            handler?.Invoke(user);
        }

        public event UserEventHandler UserUpdated;
        protected virtual void OnUserUpdated(EventArgs e, User user)
        {
            var handler = UserUpdated;
            handler?.Invoke(user);
        }

        public event UserEventHandler UserRemoved;
        protected virtual void OnUserRemoved(EventArgs e, User user)
        {
            var handler = UserRemoved;
            handler?.Invoke(user);
        }
        #endregion

        #region queries
        /// <summary>
        /// Checks if a username can be applied to a new or existing user.
        /// </summary>
        /// <param name="userName">The new username desired.</param>
        public async Task<UserNameValidationResult> IsUserNameValidAsync(string userName)
        {
            if (string.IsNullOrEmpty(userName) || userName.Length < int.Parse(ConfigurationManager.AppSettings["LB.Usernames.MinLength"]))
                return UserNameValidationResult.InvalidTooShort;

            if (userName.Length > int.Parse(ConfigurationManager.AppSettings["LB.Usernames.MaxLength"]))
                return UserNameValidationResult.InvalidTooLong;

            if (!Regex.IsMatch(userName, @"^[a-zA-Z0-9\- ]+$"))
                return UserNameValidationResult.InvalidCharacters;

            if (UserNameContainsReservedWords(userName))
                return UserNameValidationResult.InvalidReservedWord;

            using (var db = new ForumContext())
            {
                var unique = !await db.Users.AnyAsync(q => q.UserName.Equals(userName, StringComparison.InvariantCultureIgnoreCase));
                if (!unique)
                    return UserNameValidationResult.InvalidAlreadyInUse;
            }

            return UserNameValidationResult.Valid;
        }

        /// <summary>
        /// Searches for users by matching the search term with the beginning of usernames or email addresses exactly.
        /// </summary>
        public async Task<UsersContainer> SearchUsersByUsernameOrEmailAsync(string term, int maxResults = 10, int startIndex = 0)
        {
            using (var db = new ForumContext())
            {
                var userIds = await db.Users.
                    Where(q => q.UserName.StartsWith(term) || q.Email.Equals(term)).
                    OrderBy(q => q.UserName).
                    Skip(startIndex).
                    Take(maxResults).Select(q => q.Id).ToListAsync();

                var container = new UsersContainer { TotalUsers = userIds.Count };
                foreach (var id in userIds)
                    container.Users.Add(await GetUserAsync(id));

                return container;
            }
        }

        /// <summary>
        /// Searches for users by matching the search term with the beginning of usernames.
        /// </summary>
        public async Task<List<string>> SearchUsersByUsernameAsync(string term, int maxResults = 10)
        {
            using (var db = new ForumContext())
            {
                var ids = await db.Users.Where(q => q.UserName.StartsWith(term)).Take(maxResults).Select(q => q.Id).ToListAsync();
                return ids.Count == 0 ? null : ids;
            }
        }
        #endregion

        #region crud
        public User GetUser(string id)
        {
            var user = (User)ForumServer.Instance.Cache.Get(User.GetCacheKey(id));
            if (user != null)
                return user;

            using (var db = new ForumContext())
            {
                user = db.Users.SingleOrDefault(q => q.Id.Equals(id));
                if (user == null)
                    return null;

                ForumServer.Instance.Cache.Add(user);
                return user;
            }
        }

        public async Task<User> GetUserAsync(string id)
        {
            var user = (User)ForumServer.Instance.Cache.Get(User.GetCacheKey(id));
            if (user != null)
                return user;

            using (var db = new ForumContext())
            {
                user = await db.Users.SingleOrDefaultAsync(q => q.Id.Equals(id));
                if (user == null)
                    return null;

                ForumServer.Instance.Cache.Add(user);
                return user;
            }
        }

        /// <summary>
        /// Gets a list of the latest users to have signed up and pages the results.
        /// </summary>
        public async Task<UsersContainer> GetLatestUsersAsync(int maxResults = 10, int startIndex = 0)
        {
            using (var db = new ForumContext())
            {
                var container = new UsersContainer { TotalUsers = await db.Users.CountAsync() };
                if (container.TotalUsers == 0)
                    return null;

                var userIds = await db.Users.
                    OrderByDescending(q => q.Created).
                    Skip(startIndex).
                    Take(maxResults).Select(q => q.Id).ToListAsync();

                foreach (var id in userIds)
                    container.Users.Add(await GetUserAsync(id));

                return container;
            }
        }

        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var user = ForumServer.Instance.Cache.GetUserByUsername(username);
            if (user != null)
                return user;

            using (var db = new ForumContext())
            {
                user = await db.Users.SingleOrDefaultAsync(q => q.UserName.Equals(username));
                if (user == null)
                    return null;

                ForumServer.Instance.Cache.Add(user);
                return user;
            }
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            var user = ForumServer.Instance.Cache.GetUserByEmail(email);
            if (user != null)
                return user;

            using (var db = new ForumContext())
            {
                user = await db.Users.SingleOrDefaultAsync(q => q.Email.Equals(email));
                if (user == null)
                    return null;

                ForumServer.Instance.Cache.Add(user);
                return user;
            }
        }

        /// <summary>
        /// Retrieves extended information for a user that is not initially retrieved with a user due to the cost associated with querying for the data and the fact this data isn't needed for most initial retrievals.
        /// </summary>
        public async Task GetUserExtendedInformationAsync(User user)
        {
            using (var db = new ForumContext())
            {
                user.TopicsCount = await db.Posts.CountAsync(q => q.UserId.Equals(user.Id) && !q.PostId.HasValue && q.Status != PostStatus.Removed);
                user.RepliesCount = await db.Posts.CountAsync(q => q.UserId.Equals(user.Id) && q.PostId.HasValue && q.Status != PostStatus.Removed);
                user.ModerationsCount = await db.PostModerationHistoryItems.Join(db.Posts, pmhi => pmhi.PostId, p => p.Id, (pmhi, p) => new { PostModerationHistoryItem = pmhi, Post = p }).CountAsync(q => q.Post.UserId.Equals(user.Id) && q.Post.Status != PostStatus.Removed);
                user.Logins.AddRange(await db.UserLogins.Where(q => q.UserId.Equals(user.Id)).Select(q => q.LoginProvider).ToListAsync());

                // photos count will be the sum of post attachments (legacy) and photos
                user.PhotosCount = await db.PostAttachments.Join(db.Posts, pa => pa.PostId, p => p.Id, (pa, p) => new { PostAttachment = pa, Post = p }).CountAsync(q => q.Post.UserId.Equals(user.Id) && q.Post.Status != PostStatus.Removed);
                user.PhotosCount += await db.Photos.CountAsync(q => q.UserId.Equals(user.Id) && q.PostId.HasValue);
            }
        }

        /// <summary>
        ///  Use this method to update profile information only. 
        ///  Will not update email addresses or usernames as those have dedicated methods.
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrEmpty(user.Id))
                throw new ArgumentException("This method cannot be used to persist new users, you must use ASP.NET Identity in the LBV6 project for that.");

            using (var db = new ForumContext())
            {
                var dbUser = await db.Users.SingleOrDefaultAsync(q => q.Id.Equals(user.Id));
                if (dbUser == null)
                    throw new ArgumentException("No such user found in the database.");

                dbUser.FirstName = user.FirstName;
                dbUser.LastName = user.LastName;
                dbUser.AgeMin = user.AgeMin;
                dbUser.AgeMax = user.AgeMax;
                dbUser.Gender = user.Gender;
                dbUser.DateOfBirth = user.DateOfBirth;
                dbUser.Biography = user.Biography;
                dbUser.Occupation = user.Occupation;
                dbUser.Tagline = user.Tagline;
                dbUser.Status = user.Status;
                dbUser.ProfilePhotoVersion = user.ProfilePhotoVersion;
                dbUser.Verified = user.Verified;
                dbUser.CoverPhotoContentType = user.CoverPhotoContentType;
                dbUser.CoverPhotoId = user.CoverPhotoId;
                dbUser.CoverPhotoWidth = user.CoverPhotoWidth;
                dbUser.CoverPhotoHeight = user.CoverPhotoHeight;
                dbUser.TotalVists = user.TotalVists;
                dbUser.LastVisit = user.LastVisit;
                dbUser.UnreadMessagesCount = user.UnreadMessagesCount;

                // preferences
                dbUser.ReceiveNewsletters = user.ReceiveNewsletters;
                dbUser.NewTopicNotifications = user.NewTopicNotifications;
                dbUser.NewReplyNotifications = user.NewReplyNotifications;
                dbUser.NewMessageNotifications = user.NewMessageNotifications;

                await db.SaveChangesAsync();
            }

            // invalidate any cached instance of this user
            ForumServer.Instance.Cache.Remove(user.CacheKey);

            // cause any post-processing to occur
            OnUserUpdated(EventArgs.Empty, user);
        }

        /// <summary>
        /// Changes a users UserName. Performs a check before changing to ensure the desired new UserName is free.
        /// </summary>
        /// <param name="userId">The unique-identifier for the user, i.e. their GUID.</param>
        /// <param name="userName">The new UserName desired.</param>
        /// <returns>UserNameValidationResult.Valid if the change was successful, other values indicate why the username was invalid.</returns>
        public async Task<UserNameValidationResult> UpdateUserNameAsync(string userId, string userName)
        {
            // validate the username...
            var usernameValidity = await IsUserNameValidAsync(userName);
            if (usernameValidity != UserNameValidationResult.Valid)
                return usernameValidity;

            using (var db = new ForumContext())
            {
                var dbUser = await db.Users.SingleAsync(q => q.Id.Equals(userId));
                dbUser.UserName = userName;
                await db.SaveChangesAsync();

                ForumServer.Instance.Telemetry.TrackEvent("UserName Changed");

                // invalidate any cached instance of this user so the latest version is retrieved next time
                ForumServer.Instance.Cache.Remove(dbUser.CacheKey);

                // cause any post-processing to occur
                OnUserUpdated(EventArgs.Empty, dbUser);

                return UserNameValidationResult.Valid;
            }
        }

        /// <summary>
        /// Changes a users email address. As a result the email address is marked as not confirmed, causing the user to have to confirm it when they next attempt to logon.
        /// </summary>
        /// <param name="userId">The unique identifier for the user, i.e. their GUID.</param>
        /// <param name="emailAddress">The new email address desired.</param>
        public async Task<bool> UpdateEmailAddressAsync(string userId, string emailAddress)
        {
            using (var db = new ForumContext())
            {
                // is the new email in use by another user?
                if (await db.Users.AnyAsync(q => q.Email.Equals(emailAddress, StringComparison.InvariantCultureIgnoreCase)))
                    return false;

                var dbUser = await db.Users.SingleAsync(q => q.Id.Equals(userId));
                dbUser.Email = emailAddress;
                dbUser.EmailConfirmed = false;
                await db.SaveChangesAsync();

                ForumServer.Instance.Telemetry.TrackEvent("Email Address Changed");

                // invalidate any cached instance of this user so the latest version is retrieved next time
                ForumServer.Instance.Cache.Remove(dbUser.CacheKey);

                // cause any post-processing to occur
                OnUserUpdated(EventArgs.Empty, dbUser);

                return true;
            }
        }

        /// <summary>
        /// Manually removes a userLogin, i.e. for Facebook or Google from a users account.
        /// </summary>
        /// <param name="userId">The unique identifier for the user.</param>
        /// <param name="providerName">The name of the login provider, i.e. Facebook or GooglePlus.</param>
        public async Task RemoveUserLoginAsnc(string userId, string providerName)
        {
            using (var db = new ForumContext())
            {
                var dbUserLogin = await db.UserLogins.SingleOrDefaultAsync(q =>
                                q.UserId.Equals(userId) &&
                                q.LoginProvider.Equals(providerName, StringComparison.CurrentCultureIgnoreCase));

                if (dbUserLogin != null)
                {
                    db.UserLogins.Remove(dbUserLogin);
                    await db.SaveChangesAsync();

                    // invalidate any cached instance of this user so the latest version is retrieved next time
                    ForumServer.Instance.Cache.Remove(User.GetCacheKey(userId));

                    // cause any post-processing to occur
                    var user = await GetUserAsync(userId);
                    OnUserUpdated(EventArgs.Empty, user);
                }
            }
        }
        #endregion

        #region stats
        public async Task<UserStats> GetUserStatsAsync()
        {
            using (var db = new ForumContext())
            {
                return new UserStats
                {
                    TotalUsers = await db.Users.CountAsync(),
                    EnabledUsers = await db.Users.CountAsync(q => q.Status == UserStatus.Active),
                    SuspendedUsers = await db.Users.CountAsync(q => q.Status == UserStatus.Suspended),
                    BannedUsers = await db.Users.CountAsync(q => q.Status == UserStatus.Banned),
                    ConfirmedUsers = await db.Users.CountAsync(q => q.EmailConfirmed),
                    FacebookLogins = await db.UserLogins.CountAsync(q => q.LoginProvider == "Facebook"),
                    GoogleLogins = await db.UserLogins.CountAsync(q => q.LoginProvider == "GooglePlus")
                };
            }
        }
        #endregion

        #region external interactions
        /// <summary>
        /// Fills in missing values in our user profiles from Facebook - doesn't overwrite existing values
        /// </summary>
        /// <param name="accessToken">The access token specific to a user needed to access the users private details.</param>
        /// <param name="user">Our user to add missing data to from Facebook.</param>
        public async Task UpdateUserFromFacebookAsync(User user, string accessToken)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));
                if (string.IsNullOrEmpty(accessToken))
                    throw new ArgumentNullException(nameof(accessToken));

                var tries = 0;
                const int maxTries = 3;
                dynamic fbUser = null;
                var client = new FacebookClient(accessToken);

                while (fbUser == null && tries < maxTries)
                {
                    try
                    {
                        fbUser = await client.GetTaskAsync("me");
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(GetType().FullName, ex);
                    }
                    finally
                    {
                        tries++;
                    }
                }

                if (fbUser == null)
                {
                    Logging.LogDebug(GetType().FullName, $"Couldn't get user after {maxTries} attempts.");
                    return;
                }

                Logging.LogDebug(GetType().FullName, "Got FB user, about to populate our user...");

                // these properties are specific to the permissions we have requested
                // via the londonbikers Facebook app. to add more you need modify the request permissions in Facebook.
                // https://developers.facebook.com/docs/reference/api/user/ for more info

                Logging.LogDebug(GetType().FullName, "Trying first_name");
                if (string.IsNullOrEmpty(user.FirstName) && !string.IsNullOrEmpty(fbUser.first_name))
                    user.FirstName = fbUser.first_name;

                Logging.LogDebug(GetType().FullName, "Trying last_name");
                if (string.IsNullOrEmpty(user.LastName) && !string.IsNullOrEmpty(fbUser.last_name))
                    user.LastName = fbUser.last_name;

                Logging.LogDebug(GetType().FullName, "Trying age_range.min");
                if (!user.AgeMin.HasValue && fbUser.age_range != null && fbUser.age_range.min != null)
                    user.AgeMin = fbUser.age_range.min;

                Logging.LogDebug(GetType().FullName, "Trying age_range.max");
                if (!user.AgeMax.HasValue && fbUser.age_range != null && fbUser.age_range.max != null)
                    user.AgeMax = fbUser.age_range.max;

                Logging.LogDebug(GetType().FullName, "Trying about");
                if (string.IsNullOrEmpty(user.Biography) && !string.IsNullOrEmpty(fbUser.about))
                    user.Biography = fbUser.about;

                Logging.LogDebug(GetType().FullName, "Trying bio");
                if (string.IsNullOrEmpty(user.Biography) && !string.IsNullOrEmpty(fbUser.bio))
                    user.Biography = fbUser.bio;

                Logging.LogDebug(GetType().FullName, "Trying gender");
                if (!user.Gender.HasValue && !string.IsNullOrEmpty(fbUser.gender))
                {
                    if (fbUser.gender.Equals("male"))
                        user.Gender = Gender.Male;
                    else if (fbUser.gender.Equals("female"))
                        user.Gender = Gender.Female;
                }

                Logging.LogDebug(GetType().FullName, "Trying birthday");
                if (!user.DateOfBirth.HasValue && !string.IsNullOrEmpty(fbUser.birthday))
                    user.DateOfBirth = DateTime.ParseExact(fbUser.birthday, "mm/dd/yyyy", null);

                // we could extend this to get back the verified status so we can use that to enhance our spam-protection by
                // requiring non-validated users to go through additional confirmation steps here, such as captcha and email confirmation.

                // persist our changes
                await UpdateUserAsync(user);
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                throw;
            }
        }

        /// <summary>
        /// Fills in missing values in our user profiles from Google - doesn't overwrite existing values
        /// </summary>
        public async Task UpdateUserFromGoogleProfileAsync(User user, string accessToken)
        {
            try
            {
                if (user == null) throw new ArgumentNullException(nameof(user));
                if (string.IsNullOrEmpty(accessToken))
                    throw new ArgumentNullException(nameof(accessToken));

                var profile = await ForumServer.Instance.Users.GetUserProfileFromGoogleAsync(accessToken);
                await UpdateUserFromGoogleProfileAsync(user, profile);
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                throw;
            }
        }

        /// <summary>
        /// Fills in missing values in our user profiles from Google - doesn't overwrite existing values
        /// </summary>
        public async Task UpdateUserFromGoogleProfileAsync(User user, GooglePlusUserProfile profile)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Name != null)
            {
                if (string.IsNullOrEmpty(user.FirstName)) user.FirstName = profile.Name.GivenName;
                if (string.IsNullOrEmpty(user.LastName)) user.LastName = profile.Name.FamilyName;
            }

            if (!string.IsNullOrEmpty(profile.Gender) && !user.Gender.HasValue)
            {
                if (profile.Gender.Equals("male", StringComparison.InvariantCultureIgnoreCase))
                    user.Gender = Gender.Male;
                else if (profile.Gender.Equals("female", StringComparison.InvariantCultureIgnoreCase))
                    user.Gender = Gender.Female;
            }

            if (!string.IsNullOrEmpty(profile.AboutMe) && string.IsNullOrEmpty(user.Biography))
            {
                // Google has some html mark-up encoded in Unicode characters, we need to convert this to html and then drop the html.
                var aboutMe = profile.AboutMe.Replace(@"\u003c", "<").Replace(@"\u003e", ">");
                user.Biography = Utilities.RemoveHtml(aboutMe);
            }

            if (!string.IsNullOrEmpty(profile.Birthday) && !user.DateOfBirth.HasValue)
                user.DateOfBirth = DateTime.Parse(profile.Birthday);

            // persist our changes
            await UpdateUserAsync(user);
        }

        /// <summary>
        /// Downloads a users profile information from Google Plus.
        /// </summary>
        /// <param name="accessToken">The access token granted during the authorisation workflow to allow us to download the private information from Google.</param>
        public async Task<GooglePlusUserProfile> GetUserProfileFromGoogleAsync(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
                throw new ArgumentNullException(nameof(accessToken));

            // using the non-Google .NET client approach as there's an issue getting an access token from the login process that works with the client.
            GooglePlusUserProfile profile = null;
            var url = $"https://www.googleapis.com/plus/v1/people/me?access_token={Uri.EscapeDataString(accessToken)}";
            using (var client = new HttpClient())
            {
                // retry the request a few times if it's not successful
                for (var i = 0; i < 2; i++)
                {
                    var responseTask = await client.GetAsync(url);
                    if (!responseTask.IsSuccessStatusCode)
                        continue;

                    var responseStringTask = await responseTask.Content.ReadAsStringAsync();
                    profile = JsonConvert.DeserializeObject<GooglePlusUserProfile>(responseStringTask);
                    if (profile == null)
                        Logging.LogWarning(GetType().FullName, $"Couldn't get a Google profile using that accessToken: {accessToken}. Attempt: {i}");
                    else
                        break;
                }
            }

            return profile;
        }

        /// <summary>
        /// As users are created by ASP.NET Identity and not by the ForumServer, the ForumServer needs to be told when a new
        /// user is created so that it can update caches, provide real-time updates, etc.
        /// </summary>
        /// <param name="userId">The unique-identifier for the new user, i.e. the GUID.</param>
        public async Task NotifyOfNewUserAsync(string userId)
        {
            var user = await GetUserAsync(userId);
            OnUserCreated(EventArgs.Empty, user);
        }

        /// <summary>
        /// As users can be updated via ASP.NET Identity and not by the ForumServer, the ForumServer needs to be told when a new
        /// user is updated outside of its scope so that it can update caches, provide real-time updates, etc.
        /// </summary>
        /// <param name="userId">The unique-identifier for the updated user, i.e. the GUID.</param>
        public async Task NotifyOfUpdatedUserAsync(string userId)
        {
            var user = await GetUserAsync(userId);
            OnUserUpdated(EventArgs.Empty, user);
        }
        #endregion

        #region unwanted registration prevention
        /// <summary>
        /// Use when a user signs-in to collect information that can later be used to help prevent banned users from signing back up again.
        /// </summary>
        public void TrackSignin(string ipAddress, User user)
        {
            // track visit against the user
            user.TotalVists++;
            user.LastVisit = DateTime.UtcNow;
            UpdateUserAsync(user).Wait();

            // now record the IP address the user is accessing from...
            // set-up the connection to the Azure Table we use to store the sign-in summaries
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Azure.Storage.ConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("SignIns");
            table.CreateIfNotExists();

            // has the user signed-in from this IP address before?
            var retrieveOperation = TableOperation.Retrieve<UserSignInSummary>(ipAddress, user.Id);
            var retrieveResult = table.Execute(retrieveOperation);

            if (retrieveResult?.Result != null)
            {
                // they have. increment the count from this IP address
                var userSignInSummary = (UserSignInSummary)retrieveResult.Result;
                userSignInSummary.Count++;
                userSignInSummary.LastSignIn = DateTime.Now;

                // update the entity
                var updateOperation = TableOperation.InsertOrReplace(userSignInSummary);
                table.Execute(updateOperation);
            }
            else
            {
                // they haven't. we need to create a new summary for this IP-user pair
                var userSignInSummary = new UserSignInSummary(ipAddress, user.Id);
                var insertOperation = TableOperation.InsertOrReplace(userSignInSummary);
                table.Execute(insertOperation);
            }
        }

        /// <summary>
        /// Use when a user signs-in to collect information that can later be used to help prevent banned users from signing back up again.
        /// </summary>
        public async Task TrackSigninAsync(string ipAddress, User user)
        {
            // track visit against the user
            user.TotalVists++;
            user.LastVisit = DateTime.UtcNow;
            await UpdateUserAsync(user);

            // now record the IP address the user is accessing from...
            // set-up the connection to the Azure Table we use to store the sign-in summaries
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Azure.Storage.ConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("SignIns");
            table.CreateIfNotExists();

            // has the user signed-in from this IP address before?
            var retrieveOperation = TableOperation.Retrieve<UserSignInSummary>(ipAddress, user.Id);
            var retrieveResult = await table.ExecuteAsync(retrieveOperation);

            if (retrieveResult?.Result != null)
            {
                // they have. increment the count from this IP address
                var userSignInSummary = (UserSignInSummary)retrieveResult.Result;
                userSignInSummary.Count++;
                userSignInSummary.LastSignIn = DateTime.Now;

                // update the entity
                var updateOperation = TableOperation.Replace(userSignInSummary);
                await table.ExecuteAsync(updateOperation);
            }
            else
            {
                // they haven't. we need to create a new summary for this IP-user pair
                var userSignInSummary = new UserSignInSummary(ipAddress, user.Id);
                var insertOperation = TableOperation.Insert(userSignInSummary);
                await table.ExecuteAsync(insertOperation);
            }
        }

        /// <summary>
        /// Used to attempt to detect if a user trying to register is banned or not by inspecting their IP address.
        /// </summary>
        public async Task<bool> IsUserBannedByIpAsync(string ipAddress)
        {
            var storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("Azure.Storage.ConnectionString"));
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference("SignIns");
            await table.CreateIfNotExistsAsync();

            var query = new TableQuery<UserSignInSummary>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, ipAddress));
            var queryResult = table.ExecuteQuery(query);
            if (queryResult == null || !queryResult.Any())
                return false;

            var userIds = table.ExecuteQuery(query).Select(q => q.RowKey).ToList();
            using (var db = new ForumContext())
            {
                var haveActiveUsers = await db.Users.AnyAsync(q => userIds.Contains(q.Id) && q.Status == UserStatus.Active);
                var haveBannedOrSuspendedUsers = await db.Users.AnyAsync(q => userIds.Contains(q.Id) && q.Status != UserStatus.Active);

                if (haveActiveUsers)
                    return !haveBannedOrSuspendedUsers;

                return haveBannedOrSuspendedUsers;
            }
        }

        /// <summary>
        /// Used to attempt to detect if a user trying to register or add a new external login is banned or not by inspecting the provider key from the external identity provider.
        /// </summary>
        /// <param name="providerKey">The unique identifier for the user given to us by the external identity provider, i.e. Facebook.</param>
        /// <remarks>The Provider Key is not always an absolute unique identifier for the user. Facebook for instance makes this key app-specific.</remarks>
        public async Task<bool> IsUserBannedByProviderKey(string providerKey)
        {
            // see if the provider key is in use by any other banned users
            using (var db = new ForumContext())
            {
                var isBannedOrSuspended = false;
                var userIds = await db.UserLogins.Where(q => q.ProviderKey == providerKey).Select(q => q.UserId).ToListAsync();
                foreach (var user in db.Users.Where(q => userIds.Contains(q.Id) && q.Status != UserStatus.Active))
                {
                    isBannedOrSuspended = true;
                    Logging.LogInfo(GetType().FullName, $"Banned or suspended user detected: {user.UserName} ({user.Id}) via providerKey {providerKey}");
                }

                return isBannedOrSuspended;
            }
        }
        #endregion

        #region user statistics updates
        private static void ProcessTopicCreated(Post topic)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(topic.UserId);
            user.TopicsCount = user.TopicsCount + 1;
        }

        private static void ProcessReplyCreated(Post reply)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(reply.UserId);
            user.RepliesCount = user.RepliesCount + 1;
        }

        private static void ProcessPhotoAddedToPost(Photo photo)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(photo.UserId);
            user.PhotosCount = user.PhotosCount + 1;
        }

        private static void ProcessTopicMoved(Post topic, long oldForumId)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(topic.UserId);
            user.ModerationsCount = user.ModerationsCount + 1;
        }

        private static void ProcessTopicRemoved(Post topic)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(topic.UserId);
            user.ModerationsCount = user.ModerationsCount + 1;
            user.TopicsCount = user.TopicsCount - 1;
        }

        private static void ProcessReplyRemoved(Post reply)
        {
            // keep these non-persisted properties current in the cached user object
            var user = ForumServer.Instance.Users.GetUser(reply.UserId);
            user.ModerationsCount = user.ModerationsCount + 1;
            user.RepliesCount = user.RepliesCount - 1;
        }
        #endregion

        #region private methods
        public bool UserNameContainsReservedWords(string userName)
        {
            var prohibitedUsernamesCsv = ConfigurationManager.AppSettings["LB.ProhibitedUsernames"];
            if (string.IsNullOrEmpty(prohibitedUsernamesCsv))
                return false;

            var prohibitedUsernames = prohibitedUsernamesCsv.Split(',').ToList();
            return prohibitedUsernames.Any(prohibitedUsername => prohibitedUsername.Equals(userName, StringComparison.InvariantCultureIgnoreCase));
        }
        #endregion
    }
}