using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Exceptions;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LBV6.Api
{
    [Authorize]
    public class PhotosApiController : ApiController
    {
        #region photos
        [HttpPost]
        public async Task<IHttpActionResult> UploadPhoto()
        {
            if (!bool.Parse(ConfigurationManager.AppSettings["LB.EnablePhotoUploads"]))
                return BadRequest("Photo uploads are not currently available.");

            // validation...
            if (HttpContext.Current.Request.Files.Count < 1)
                return BadRequest("No files were uploaded.");

            if (HttpContext.Current.Request.Files.Count > 1)
                return BadRequest("Only a single file can be uploaded.");

            var file = HttpContext.Current.Request.Files[0];

            if (file.ContentLength <= 0)
                return BadRequest("File has no content.");

            if (!file.ContentType.ToLowerInvariant().StartsWith("image/"))
                return BadRequest("Only images accepted.");

            // save the photo...
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);
            var photo = await ForumServer.Instance.Photos.StorePhotoAsync(file, user);

            // return the photo id
            return Ok(photo.Id);
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeletePhoto(Guid photoId, long topicId, long replyId)
        {
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(topicId);
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);
            var post = replyId == 0 ? topic : topic.Replies.SingleOrDefault(q => q.Id.Equals(replyId));

            // validation
            if (post == null)
            {
                Logging.LogWarning(GetType().FullName, $"Post not found. topicId: {topicId}, replyId: {replyId}");
                return BadRequest("Post not found.");
            }

            var photo = post.Photos.SingleOrDefault(q => q.Id.Equals(photoId));
            if (photo == null)
            {
                Logging.LogWarning(GetType().FullName, $"Photo not found in post. topicId: {topicId}, replyId: {replyId}");
                return BadRequest("Post not found.");
            }

            // access control
            if (post.UserId != user.Id)
            {
                Logging.LogWarning(GetType().FullName, $"User ({user.Id}) does not own post ({post.Id}).");
                return BadRequest("Not authorised. User doesn't own post.");
            }

            await ForumServer.Instance.Photos.DeletePhotoAsync(post, photo);
            return Ok();
        }
        
        [HttpPost]
        public async Task<IHttpActionResult> UpdatePhoto(PhotoDto photoDto, long topicId, long replyId)
        {
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(topicId);
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);
            var post = replyId == 0 ? topic : topic.Replies.SingleOrDefault(q => q.Id.Equals(replyId));
            //var transientPhoto = Transformations.ConvertPhotoDtoToPhoto(photoDto);

            // validation
            if (post == null)
            {
                Logging.LogWarning(GetType().FullName, $"Post not found. topicId: {topicId}, replyId: {replyId}");
                return BadRequest("Post not found.");
            }

            var photo = post.Photos.SingleOrDefault(q => q.Id.Equals(photoDto.Id));
            if (photo == null)
            {
                Logging.LogWarning(GetType().FullName, $"Photo not found in post. topicId: {topicId}, replyId: {replyId}");
                return BadRequest("Post not found.");
            }

            // access control
            if (post.UserId != user.Id)
            {
                Logging.LogWarning(GetType().FullName, $"User ({user.Id}) does not own post ({post.Id}).");
                return BadRequest("Not authorised. User doesn't own post.");
            }

            photo.Credit = photoDto.Credit;
            photo.Caption = photoDto.Caption;

            await ForumServer.Instance.Photos.UpdatePhotoAsync(post, photo);
            return Ok();
        }

        [HttpPost]
        public async Task<IHttpActionResult> UploadProfileCover()
        {
            if (!bool.Parse(ConfigurationManager.AppSettings["LB.EnablePhotoUploads"]))
                return BadRequest("Profile cover photo uploads are not currently available.");

            // validation...
            if (HttpContext.Current.Request.Files.Count < 1)
                return BadRequest("No files were uploaded.");

            if (HttpContext.Current.Request.Files.Count > 1)
                return BadRequest("Only a single file can be uploaded.");

            var file = HttpContext.Current.Request.Files[0];

            if (file.ContentLength <= 0)
                return BadRequest("File has no content.");

            if (!file.ContentType.ToLowerInvariant().StartsWith("image/"))
                return BadRequest("Only images accepted.");

            // save the photo...
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);

            try
            {
                await ForumServer.Instance.Photos.StoreProfileCoverPhotoAsync(file, user);
            }
            catch (PhotoTooSmallException ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok(Transformations.ConvertUserToUserProfileDto(user));
        }

        [HttpDelete]
        public async Task<IHttpActionResult> DeleteCoverPhoto()
        {
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);
            await ForumServer.Instance.Photos.DeleteProfileCoverPhotoAsync(user);
            return Ok(Transformations.ConvertUserToUserProfileDto(user));
        }

        /// <summary>
        /// Used to invalidate any notifications for this photo.
        /// </summary>
        [HttpPut]
        public async Task<IHttpActionResult> PhotoViewed(Guid photoId)
        {
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync(User.Identity.Name);
            await ForumServer.Instance.Notifications.InvalidatePhotoViewNotificationAsync(user.Id, photoId);
            Debug.WriteLine("PhotoViewed: " + photoId);
            return Ok();
        }
        #endregion

        #region photo comments
        /// <summary>
        /// Creates a new, or updates an existing photo comment.
        /// </summary>
        /// <param name="photoComment">The PhotoComment to create or update.</param>
        /// <param name="topicId">The topic id that the photo this comment relates to is always required. Even if the photo is for a reply, the topic id is required.</param>
        [HttpPost]
        public async Task<IHttpActionResult> UpdatePhotoComment(PhotoComment photoComment, long topicId)
        {
            if (photoComment == null)
                return BadRequest("Null PhotoComment!");

            if (!photoComment.IsValid())
                return BadRequest("Invalid PhotoComment!");
            
            // override the user-id and created date to prevent client hacks
            photoComment.UserId = User.Identity.GetUserId();
            if (photoComment.Id < 1)
                photoComment.Created = DateTime.Now;

            await ForumServer.Instance.Photos.UpdatePhotoCommentAsync(photoComment, topicId);
            return Ok(photoComment.Id);
        }

        /// <summary>
        /// Deletes an existing photo comment.
        /// </summary>
        /// <param name="photoCommentId">The id of the PhotoComment to delete.</param>
        /// <param name="topicId">The topic id that the photo this comment relates to is always required. Even if the photo is for a reply, the topic id is required.</param>
        [HttpDelete]
        public async Task<IHttpActionResult> DeletePhotoComment(long photoCommentId, long topicId)
        {
            await ForumServer.Instance.Photos.DeletePhotoCommentAsync(photoCommentId, topicId, User.Identity.GetUserId());
            return Ok();
        }
        #endregion
    }
}