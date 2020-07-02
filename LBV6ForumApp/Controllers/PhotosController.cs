using LBV6Library;
using LBV6Library.Exceptions;
using LBV6Library.Models;
using net.openstack.Core.Domain;
using net.openstack.Core.Exceptions.Response;
using net.openstack.Core.Providers;
using net.openstack.Providers.Rackspace;
using System;
using System.Configuration;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace LBV6ForumApp.Controllers
{
    /// <summary>
    /// Facilitates photo model and file operations (with the OVH Cloud object storage).
    /// </summary>
    public class PhotosController
    {
        #region accessors
        private Timer DeleteUnusedFilesTimer { get; }
        public string PhotosContainer { get; }
        private string CoversContainer { get; }
        private string ProfilesContainer { get; }
        public string PrivateMessagePhotosContainer { get; }
        #endregion

        #region constructors
        internal PhotosController()
        {
            // some tasks need to run continuously every now and then...
            if (bool.Parse(ConfigurationManager.AppSettings["LB.EnablePhotoOrphanDeletions"]))
                DeleteUnusedFilesTimer = new Timer(async delegate { await DeleteUnusedPhotosAsync(); }, null, TimeSpan.FromMinutes(5), TimeSpan.FromDays(1));

            PhotosContainer = ConfigurationManager.AppSettings["OVH.OpenStack.FileContainer"];
            CoversContainer = ConfigurationManager.AppSettings["OVH.OpenStack.CoverFileContainer"];
            ProfilesContainer = ConfigurationManager.AppSettings["OVH.OpenStack.ProfilesContainer"];
            PrivateMessagePhotosContainer = ConfigurationManager.AppSettings["OVH.OpenStack.PrivateMessagePhotosContainer"];
        }
        #endregion

        #region photos
        public async Task<Photo> StorePhotoAsync(HttpPostedFile file, LBV6Library.Models.User user)
        {
            // task:
            // - create and persist model
            // - persist file to OVH cloud
            // - return model

            // determine the image dimensions
            Size size;
            using (var image = Image.FromStream(file.InputStream))
                size = Utilities.GetAutoRotatedImageDimensions(image);

            var photo = new Photo
            {
                ContentType = file.ContentType,
                Length = file.ContentLength,
                UserId = user.Id,
                Width = size.Width,
                Height = size.Height
            };

            using (var db = new ForumContext())
            {
                db.Photos.Add(photo);
                await db.SaveChangesAsync();
            }

            // make sure we're back at position 0 after opening as an image
            file.InputStream.Seek(0, SeekOrigin.Begin);
            var files = GetFilesProvider();
            using (var stream = file.InputStream)
                files.CreateObject(PhotosContainer, stream, photo.Id.ToString());

            return photo;
        }

        /// <summary>
        /// Photos are created individually and then have to be associated with a post to go live. 
        /// This method will perform the association with the post.
        /// </summary>
        public async Task AddPhotoToPostAsync(Post post, Guid photoId)
        {
            Photo photo;
            using (var db = new ForumContext())
            {
                photo = await db.Photos.SingleOrDefaultAsync(q => q.Id.Equals(photoId));
                if (photo == null)
                {
                    Logging.LogWarning(GetType().FullName, $"Photo {photoId} couldn't be found to add to post {post.Id}.");
                    return;
                }

                // access control: check that the photo is owned by the post author
                if (!photo.UserId.Equals(post.UserId))
                    throw new NotAuthorisedException("The photo is authored by another user than the post author.");

                photo.PostId = post.Id;
                await db.SaveChangesAsync();
                post.Photos.Add(photo);
            }

            // remove the topic from the cache so the next time it's retrieved it's retrieved with the photo associations.
            string topicCacheKey;
            if (!post.IsTopic && post.PostId.HasValue)
                topicCacheKey = Post.GetCacheKey(post.PostId.Value);
            else
                topicCacheKey = post.CacheKey;
            ForumServer.Instance.Cache.Remove(topicCacheKey);

            OnPhotoAddedToPost(EventArgs.Empty, photo);
        }

        /// <summary>
        /// Performs updates on select attributes of a photo object, i.e. caption.
        /// </summary>
        public async Task UpdatePhotoAsync(Post post, Photo transientPhoto)
        {
            using (var db = new ForumContext())
            {
                var dbPhoto = await db.Photos.SingleOrDefaultAsync(q => q.Id.Equals(transientPhoto.Id));
                if (dbPhoto == null)
                {
                    Logging.LogWarning(GetType().FullName, $"Photo {transientPhoto.Id} couldn't be found to update.");
                    return;
                }

                db.Entry(dbPhoto).CurrentValues.SetValues(transientPhoto);
                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes the photo file from object storage and remove from the domain object.
        /// </summary>
        public async Task DeletePhotoAsync(Post post, Photo photo)
        {
            var files = GetFilesProvider();
            files.DeleteObject(PhotosContainer, photo.Id.ToString());

            // assumption: post comes from the cache
            // remove photo from post in cached instance
            post.Photos.RemoveAll(q => q.Id.Equals(photo.Id));

            // remove photo from database
            using (var db = new ForumContext())
            {
                var dbPhoto = await db.Photos.SingleOrDefaultAsync(q => q.Id.Equals(photo.Id));
                if (dbPhoto != null)
                {
                    db.Photos.Remove(dbPhoto);
                    await db.SaveChangesAsync();
                }
            }
        }
        #endregion

        #region photo comments
        /// <summary>
        /// Creates a new, or updates an existing photo comment.
        /// </summary>
        /// <param name="photoComment">The PhotoComment to create or update.</param>
        /// <param name="topicId">The topic id that the photo this comment relates to is always required. Even if this is for a reply photo, we need the topic id.</param>
        public async Task UpdatePhotoCommentAsync(PhotoComment photoComment, long topicId)
        {
            if (photoComment == null)
                throw new ArgumentNullException(nameof(photoComment));

            if (!photoComment.IsValid())
                throw new ArgumentException("PhotoComment is invalid!");

            // resolve dependencies
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(topicId);
            if (topic == null)
                throw new ArgumentException("topicId is invalid. No such topic found.");

            Post reply = null;
            var photo = Utilities.FindPhoto(topic, photoComment.PhotoId);
            if (photo.PostId != topic.Id)
                reply = topic.Replies.Single(q => q.Id.Equals(photo.PostId));

            // okay that would be bad...
            if (photo == null)
                throw new ArgumentException("No such photo found for the photo comment!");
            
            using (var db = new ForumContext())
            {
                var isNew = false;

                // is this a new, or existing photo comment?
                if (photoComment.Id < 1)
                {
                    // new, persist it
                    db.PhotoComments.Add(photoComment);
                    isNew = true;
                }
                else
                {
                    // existing. we just want to update some attributes, so retrieve from the database
                    var dbPhotoComment = await db.PhotoComments.SingleOrDefaultAsync(q => q.Id.Equals(photoComment.Id));
                    if (dbPhotoComment == null)
                        throw new ArgumentException("No such photo comment exists!");

                    if (!dbPhotoComment.UserId.Equals(photoComment.UserId))
                        throw new ArgumentException("Photo comment not owned by purported user!");

                    // only update the comment text
                    dbPhotoComment.Text = photoComment.Text;
                }

                await db.SaveChangesAsync();
                if (isNew)
                    OnCommentAddedToPhoto(EventArgs.Empty, photo.Id, photoComment.Id, topic, reply);
            }

            // -- cache management
            // don't invalidate the post/photo this comment relates to, that's too heavy-handed and would limit performance with unnecessary database reads
            // update the photo model instead

            // is this a root comment, or a nested one? if nested, find the parent comment and add it
            if (!photoComment.ParentCommentId.HasValue)
            {
                var index = photo.Comments.FindIndex(q => q.Id.Equals(photoComment.Id));
                if (index == -1)
                {
                    // new comment, add it
                    photo.Comments.Add(photoComment);
                }
                else
                {
                    // existing comment, replace the comment
                    photo.Comments.RemoveAt(index);
                    photo.Comments.Insert(index, photoComment);
                }
            }
            else
            {
                // find the parent comment
                PhotoComment parentPhotoComment = null;
                foreach (var pc in photo.Comments)
                {
                    if (pc.Id.Equals(photoComment.ParentCommentId.Value))
                    {
                        parentPhotoComment = pc;
                        break;
                    }

                    if (pc.ChildComments.Any())
                        parentPhotoComment = Utilities.FindPhotoComment(pc.ChildComments, photoComment.ParentCommentId.Value);
                }

                if (parentPhotoComment == null)
                    throw new Exception("Parent Comment id: " + photoComment.ParentCommentId.Value + " couldn't be found on PhotoComment " + photoComment.Id);

                // we've got the parent photo comment, update or add the comment
                var index = parentPhotoComment.ChildComments.FindIndex(q => q.Id.Equals(photoComment.Id));
                if (index == -1)
                {
                    // new comment, add it
                    parentPhotoComment.ChildComments.Add(photoComment);
                }
                else
                {
                    // existing comment, replace the comment
                    parentPhotoComment.ChildComments.RemoveAt(index);
                    parentPhotoComment.ChildComments.Insert(index, photoComment);
                }
            }
        }

        /// <summary>
        /// Deletes an existing photo comment for a photo found in either a reply or a topic.
        /// </summary>
        /// <param name="photoCommentId">The id for the photo comment to delete.</param>
        /// <param name="topicId">The id for the topic that the photo either resides in directly or via a reply of the topic. This is always required so we know where to find the reply if necessary.</param>
        /// <param name="userIdRequestingDeletion">The id of the user requesting the deletion. This is used to ensure the person who owns the photo comment is also the one requesting its deletion. </param>
        public async Task DeletePhotoCommentAsync(long photoCommentId, long topicId, string userIdRequestingDeletion)
        {
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(topicId);
            if (topic == null)
                throw new ArgumentNullException(nameof(topicId));

            // remove the photo comment from the database
            using (var db = new ForumContext())
            {
                var dbPhotoComment = await db.PhotoComments.SingleOrDefaultAsync(q => q.Id.Equals(photoCommentId));
                if (dbPhotoComment != null)
                {
                    db.PhotoComments.Remove(dbPhotoComment);
                    await db.SaveChangesAsync();
                }
            }

            // remove the photo comment from the cached model
            // find the parent photo
            var photo = Utilities.FindPhotoForPhotoComment(topic, photoCommentId);
            var removed = photo?.Comments.RemoveAll(q => q.Id.Equals(photoCommentId));
            if (removed < 1)
                Logging.LogWarning(GetType().FullName, $"Deleted PhotoComment {photoCommentId} from the database but didn't remove anything from the cache.");
        }
        #endregion

        #region cover photos

        public async Task StoreProfileCoverPhotoAsync(HttpPostedFile file, LBV6Library.Models.User user)
        {
            // make sure the image is big enough
            var minimumSizeParts = ConfigurationManager.AppSettings["LB.MinimumProfileCoverPhotoDimensions"].Split('x');
            var minimumSize = new Size(int.Parse(minimumSizeParts[0]), int.Parse(minimumSizeParts[1]));
            Size size;
            using (var image = Image.FromStream(file.InputStream))
                size = Utilities.GetAutoRotatedImageDimensions(image);

            if (size.Width < minimumSize.Width || size.Height < minimumSize.Height)
                throw new PhotoTooSmallException(
                    $"Photo needs to be a minimum {ConfigurationManager.AppSettings["LB.MinimumProfileCoverPhotoDimensions"]} in size.");

            var files = GetFilesProvider();

            // remove any existing cover image from the file store
            if (user.CoverPhotoId.HasValue)
                files.DeleteObject(CoversContainer, user.CoverPhotoId.Value.ToString());

            // update the user object
            user.CoverPhotoContentType = file.ContentType;
            user.CoverPhotoId = Guid.NewGuid();
            user.CoverPhotoWidth = size.Width;
            user.CoverPhotoHeight = size.Height;
            await ForumServer.Instance.Users.UpdateUserAsync(user);

            // make sure we're back at position 0 after opening as an image
            file.InputStream.Seek(0, SeekOrigin.Begin);
            using (var stream = file.InputStream)
                files.CreateObject(CoversContainer, stream, user.CoverPhotoId.ToString());
        }

        public async Task DeleteProfileCoverPhotoAsync(LBV6Library.Models.User user)
        {
            // remove the existing photo file from the object store
            var files = GetFilesProvider();
            if (user.CoverPhotoId.HasValue)
                files.DeleteObject(CoversContainer, user.CoverPhotoId.Value.ToString());

            // update the user object
            user.CoverPhotoContentType = null;
            user.CoverPhotoId = null;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
        }

        #endregion

        #region profile photos
        /// <summary>
        /// Saves a new profile photo to the OVH cloud and updates the user object accordingly.
        /// </summary>
        public async Task StoreProfilePhotoAsync(MemoryStream memoryStream, LBV6Library.Models.User user)
        {
            var files = GetFilesProvider();

            // remove any existing cover image from the file store
            if (user.ProfilePhotoVersion.HasValue)
            {
                try
                {
                    files.DeleteObject(ProfilesContainer, $"{user.Id}_{user.ProfilePhotoVersion.Value - 1}");
                }
                catch (Exception ex)
                {
                    Logging.LogError(GetType().FullName, ex);
                }
            }

            // update the user object
            user.ProfilePhotoVersion = user.ProfilePhotoVersion + 1 ?? 1;
            await ForumServer.Instance.Users.UpdateUserAsync(user);

            // make sure we're back at position 0 after any prior processing
            memoryStream.Seek(0, SeekOrigin.Begin);

            // save the stream to the OVH cloud
            files.CreateObject(ProfilesContainer, memoryStream, $"{user.Id}_{user.ProfilePhotoVersion.Value}");
            memoryStream.Dispose();
        }

        public async Task DeleteProfilePhotoAsync(LBV6Library.Models.User user)
        {
            // remove the existing profile photo file from the object store
            var files = GetFilesProvider();
            if (user.ProfilePhotoVersion.HasValue)
                files.DeleteObject(ProfilesContainer, $"{user.Id}_{user.ProfilePhotoVersion.Value}");

            // update the user object
            user.ProfilePhotoVersion = null;
            await ForumServer.Instance.Users.UpdateUserAsync(user);
        }
        #endregion

        #region events
        public delegate void PhotoEventHandler(Photo photo);
        public event PhotoEventHandler PhotoAddedToPost;
        protected virtual void OnPhotoAddedToPost(EventArgs e, Photo photo)
        {
            var handler = PhotoAddedToPost;
            handler?.Invoke(photo);
        }

        public delegate void PostPhotoCommentEventHandler(Guid photoId, long photoCommentId, Post topic, Post reply = null);
        public event PostPhotoCommentEventHandler CommentAddedToPhoto;
        protected virtual void OnCommentAddedToPhoto(EventArgs e, Guid photoId, long photoCommentId, Post topic, Post reply = null)
        {
            var handler = CommentAddedToPhoto;
            handler?.Invoke(photoId, photoCommentId, topic, reply);
        }
        #endregion

        public CloudFilesProvider GetFilesProvider()
        {
            // http://www.openstacknetsdk.org/
            var urlBase = new Uri("https://auth.cloud.ovh.net/v2.0");
            var cloudIdentityWithProject = new CloudIdentityWithProject
            {
                Password = ConfigurationManager.AppSettings["OVH.OpenStack.Password"],
                Username = ConfigurationManager.AppSettings["OVH.OpenStack.Username"],
                ProjectName = ConfigurationManager.AppSettings["OVH.OpenStack.ProjectName"]
            };

            var identityProvider = new OpenStackIdentityProvider(urlBase, cloudIdentityWithProject);
            var cloudFilesProvider = new CloudFilesProvider(null, ConfigurationManager.AppSettings["OVH.OpenStack.ProjectRegion"], identityProvider, null);
            return cloudFilesProvider;
        }

        #region timer callbacks
        private async Task DeleteUnusedPhotosAsync()
        {
            try
            {
                Logging.LogDebug(GetType().FullName, "Deleting unused photos");
                var files = GetFilesProvider();

                using (var db = new ForumContext())
                {
                    // find photos not assigned to a post uploaded over a day ago
                    // also excluding legacy gallery photos to stop this from deleting photos during migration
                    var createdBefore = DateTime.UtcNow.AddDays(-1);
                    var dbPhotos = await db.Photos.Where(q => !q.PostId.HasValue && q.Created < createdBefore && !q.LegacyGalleryPhotoId.HasValue).ToListAsync();

                    foreach (var dbPhoto in dbPhotos)
                    {
                        try
                        {
                            files.DeleteObject(ConfigurationManager.AppSettings["OVH.OpenStack.FileContainer"], dbPhoto.Id.ToString());
                        }
                        catch (ItemNotFoundException)
                        {
                            Logging.LogWarning(GetType().FullName, "File not found in object store: " + dbPhoto.Id);
                        }

                        db.Photos.Remove(dbPhoto);
                    }

                    if (dbPhotos.Count > 0)
                    {
                        await db.SaveChangesAsync();
                        Logging.LogInfo(GetType().FullName, $"Deleted {dbPhotos.Count} unused photos.");
                    }
                    else
                    {
                        Logging.LogInfo(GetType().FullName, "Completed check for unused photos to remove. None found.");
                    }
                }
            }
            catch (Exception ex)
            {
                // catch here as this code is called during PostController construction and an exception there will total the app
                Logging.LogError(GetType().FullName, ex);
            }
        }
        #endregion
    }
}