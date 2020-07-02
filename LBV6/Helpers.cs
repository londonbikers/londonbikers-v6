using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.ApplicationInsights;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;

namespace LBV6
{
    /// <summary>
    /// Utility methods specific to the web-application go here.
    /// </summary>
    public static class Helpers
    {
        #region members
        public enum EventMessageType
        {
            Success,
            Information,
            Warning,
            Error
        }

        /// <summary>
        /// Represents a response that needs to be given to the user via a notification bar.
        /// </summary>
        public struct EventMessage
        {
            public EventMessageType Type { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
        }
        #endregion

        #region accessors
        public static TelemetryClient Telemetry => (TelemetryClient)HttpContext.Current.Application["Telemetry"];
        #endregion

        #region categories
        /// <summary>
        /// Converts one or more Category models into a collection that's simpler and has unecessary or sensitive properties removed, for the client to use.
        /// </summary>
        /// <param name="categories">The list of Categories to convert.</param>
        /// <param name="useExtendedForums">Some clients can view sensitive information, i.e. admins. Using this flag will enable conversion of Forum objects in Category objects to this extended version.</param>
        /// <returns>The domain objects converted to simpler objects (DTOs) for the client to use, or null if the user is not authorised to view the Category.</returns>
        public static List<CategoryDto> ConvertCategoriesToCategoryDtos(List<Category> categories, bool useExtendedForums = false)
        {
            return useExtendedForums ? categories.Select(ConvertCategoryToCategoryExtendedDto).ToList() : categories.Select(ConvertCategoryToCategoryDto).ToList();
        }

        public static CategoryDto ConvertCategoryToCategoryDto(Category category)
        {
            // I dislike this. We are not DRY'ing!

            var dto = new CategoryDto
            {
                Created = category.Created,
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Order = category.Order,
                IsGalleryCategory = category.IsGalleryCategory
            };

            foreach (var f in category.Forums.Where(CanUserAccessForum))
            {
                var forumDto = new ForumDto
                {
                    Id = f.Id,
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Created = f.Created,
                    Name = f.Name,
                    Order = f.Order,
                    PostCount = f.PostCount
                };


                if (f.LastUpdated.HasValue)
                    forumDto.LastUpdated = f.LastUpdated.Value;

                dto.Forums.Add(forumDto);
            }

            // if the user isn't an admin and the category has no forums then don't return the category, so the user
            // doesn't see the category. admins need to see empty categories for when creating categories!

            if (!HttpContext.Current.User.IsInRole(AccessControlRole.Administrator.ToString()))
            {
                // if the user wasn't authorised to see any forums in this category then there will be no forums, either way
                // we don't want to transfer any empty categories to the client.
                return dto.Forums.Count == 0 ? null : dto;
            }

            // admins see all categories
            return dto;
        }

        public static CategoryDto ConvertCategoryToCategoryExtendedDto(Category category)
        {
            // I dislike this. We are not DRY'ing!

            var dto = new CategoryExtendedDto
            {
                Created = category.Created,
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Order = category.Order,
                IsGalleryCategory = category.IsGalleryCategory
            };

            foreach (var f in category.Forums.Where(CanUserAccessForum))
            {
                var forumDto = new ForumExtendedDto
                {
                    Id = f.Id,
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    Created = f.Created,
                    Name = f.Name,
                    Order = f.Order,
                    PostCount = f.PostCount
                };

                if (f.LastUpdated.HasValue)
                    forumDto.LastUpdated = f.LastUpdated.Value;

                foreach (var far in f.AccessRoles)
                    forumDto.AccessRoles.Add(new ForumRoleDto { ForumId = f.Id, Id = far.Id, Role = far.Role });

                foreach (var fpr in f.PostRoles)
                    forumDto.PostRoles.Add(new ForumRoleDto { ForumId = f.Id, Id = fpr.Id, Role = fpr.Role });

                dto.Forums.Add(forumDto);
            }

            // if the user isn't an admin and the category has no forums then don't return the category, so the user
            // doesn't see the category. admins need to see empty categories for when creating categories!

            if (!HttpContext.Current.User.IsInRole(AccessControlRole.Administrator.ToString()))
            {
                // if the user wasn't authorised to see any forums in this category then there will be no forums, either way
                // we don't want to transfer any empty categories to the client.
                return dto.Forums.Count == 0 ? null : dto;
            }

            // admins see all categories
            return dto;
        }

        public static List<CategorySimpleDto> ConvertCategoriesToCategorySimpleDtos(List<Category> categories, bool filterOutNonPostableForums = false)
        {
            // null DTO means the user is not authorised to view the category
            var dtos = categories.Select(category => ConvertCategoryToCategorySimpleDto(category, filterOutNonPostableForums)).Where(dto => dto != null).ToList();
            return dtos;
        }

        public static CategorySimpleDto ConvertCategoryToCategorySimpleDto(Category category, bool filterOutNonPostableForums = false)
        {
            var dto = new CategorySimpleDto
            {
                Id = category.Id,
                Name = category.Name,
                IsGalleryCategory = category.IsGalleryCategory
            };

            var forums = filterOutNonPostableForums
                ? category.Forums.Where(forum => CanUserPostToForum(forum) && CanUserAccessForum(forum))
                : category.Forums.Where(CanUserAccessForum);

            foreach (var f in forums)
            {
                dto.Forums.Add(new ForumSimpleDto
                {
                    Id = f.Id,
                    Name = f.Name,
                });
            }

            // if the user wasn't authorised to see any forums in this category then there will be no forums, either way
            // we don't want to transfer any empty categories to the client.
            return dto.Forums.Count == 0 ? null : dto;
        }
        #endregion

        #region users
        public static UserProfileSelfDto ConvertUserToUserProfileSelfDto(User user)
        {
            var dto = new UserProfileSelfDto
            {
                Id = user.Id,
                UserName = user.UserName,
                ProfileFileStoreId = Utilities.GetFileStoreIdForProfilePhoto(user),
                IsModerator = HttpContext.Current.User.IsInRole("Moderator"),
                UnreadMessagesCount = user.UnreadMessagesCount,
                NonMessageNotificationsCount = user.NonMessageNotificationsCount,
                Preferences = new UserProfileSelfDto.UserPreferences
                {
                    NewMessageNotifications = user.NewMessageNotifications,
                    NewReplyNotifications = user.NewReplyNotifications,
                    NewTopicNotifications = user.NewTopicNotifications,
                    NewPhotoCommentNotifications = user.NewPhotoCommentNotifications,
                    ReceiveNewsletters = user.ReceiveNewsletters
                }
            };

            return dto;
        }

        public static List<UserProfileLightDto> ConvertUsersToUserProfileLightDtos(List<User> users)
        {
            return users.Select(Transformations.ConvertUserToUserProfileLightDto).ToList();
        }

        public static async Task<User> ConvertUserProfileLightDtoToUserAsync(UserProfileLightDto dto)
        {
            return await ForumServer.Instance.Users.GetUserAsync(dto.Id);
        }

        /// <summary>
        /// Returns a relative or absolute URL to the full-size version of a users cover photo, if they have one.
        /// </summary>
        public static string GetUserCoverPhotoUrl(User user, bool absoluteUrl)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (!user.CoverPhotoId.HasValue)
                return null;

            var root = absoluteUrl ? ConfigurationManager.AppSettings["LB.Url"] : string.Empty;
            return $"{root}/os/{Utilities.GetFileStoreIdForCoverPhoto(user)}?maxwidth=1200&c=Covers&zoom=1";
        }

        public static async Task LogUserVisitAsync(User user)
        {
            await ForumServer.Instance.Users.TrackSigninAsync(HttpContext.Current.Request.UserHostAddress, user);
        }

        public static void LogUserVisit(User user)
        {
            ForumServer.Instance.Users.TrackSignin(HttpContext.Current.Request.UserHostAddress, user);
        }

        internal static bool IsIllegalSuperUserOperation(User operationContextUser)
        {
            return !HttpContext.Current.User.Identity.GetUserName().Equals("jay", StringComparison.InvariantCultureIgnoreCase) && 
                   operationContextUser.UserName.Equals("jay", StringComparison.InvariantCultureIgnoreCase);
        }
        #endregion

        #region access-control
        public static bool CanUserAccessForum(Forum forum)
        {
            if (forum.AccessRoles.Count == 0)
                return true;

            // forum has access roles

            // without being logged in, we can't inspect your roles
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
                return false;

            // admins role provides complete access, regardless of forum access role assignments
            if (HttpContext.Current.User.IsInRole("Administrator"))
                return true;

            foreach (var accessRole in forum.AccessRoles)
            {
                if (HttpContext.Current.User.IsInRole(accessRole.Role))
                    return true;
            }

            // user did not have required role
            return false;
        }
        #endregion

        #region user interface
        /// <summary>
        /// Allows an event message to be shown to the user on the subsequent page view. Useful for if you need to
        /// perform a redirect but still wish to show a message at the resulting page.
        /// </summary>
        public static void SetEventMessage(string title, string content, EventMessageType type = EventMessageType.Information)
        {
            var response = new EventMessage { Title = title, Type = type, Content = content };
            HttpContext.Current.Session["EventMessage"] = response;
        }

        /// <summary>
        /// If an event message has been stored from the previous request then this will be returned.
        /// </summary>
        public static EventMessage? GetEventMessage()
        {
            if (HttpContext.Current.Session == null || HttpContext.Current.Session["EventMessage"] == null) return null;
            var response = (EventMessage)HttpContext.Current.Session["EventMessage"];
            HttpContext.Current.Session.Remove("EventMessage");
            return response;
        }

        /// <summary>
        /// Determines whether or not the client is using a mobile device.
        /// </summary>
        public static bool IsMobile()
        {
            return Utilities.IsMobile(HttpContext.Current.Request.UserAgent);
        }
        #endregion

        #region misc
        /// <summary>
        /// Causes a 404 Not Found response to be returned.
        /// </summary>
        public static void Return404()
        {
            HttpContext.Current.Response.Status = "404 Not Found";
            HttpContext.Current.Response.StatusCode = 404;
            HttpContext.Current.Response.Flush();
            HttpContext.Current.Response.End();
        }

        /// <summary>
        /// Checks to see if a users IP address is on the stopforumspam.com black-list.
        /// </summary>
        public static bool IsUserBlackListed(string ipAddress, string email)
        {
            var result = false;
            var url = "http://api.stopforumspam.org/api?email=" + email;

            if (!string.IsNullOrEmpty(ipAddress) && !ipAddress.Contains(":"))
                url += "&ip=" + ipAddress;

            try
            {
                var doc = XDocument.Load(url);
                result = doc.ToString().ToLowerInvariant().Contains("<appears>yes</appears");
                if (result)
                    Logging.LogInfo(typeof(Helpers).FullName, $"StopForumSpam API call: Email = {email}. IP = {ipAddress}. Result: black-listed.");
            }
            catch (Exception ex)
            {
                Logging.LogError(typeof(Helpers).FullName, $"Error with URL: {url}, {ex.Message}");
            }

            return result;
        }

        public static bool CanUserPostToForum(Forum forum)
        {
            if (forum.PostRoles == null || forum.PostRoles.Count == 0)
                return true;

            return forum.PostRoles.Any(forumPostRole => HttpContext.Current.User.IsInRole(forumPostRole.Role));
        }
        #endregion
    }
}