using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("Photos")]
    public class Photo : INotCachable
    {
        #region primary accessors
        /// <summary>
        /// The unique identifier for the photo.
        /// </summary>
        /// <remarks>
        /// We use a guid as the ID will also be the file object-store id and a guid suits that best.
        /// </remarks>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        /// <summary>
        /// When uploaded, photos are not associated with a post immediately but are upon post creation.
        /// </summary>
        public long? PostId { get; set; }
        /// <summary>
        /// The data the photo object was created, not the date the photo was captured.
        /// </summary>
        [Required]
        public DateTime Created { get; set; }
        /// <summary>
        /// If supplied, the textual caption as provided by the owning user.
        /// </summary>
        public string Caption { get; set; }
        /// <summary>
        /// If supplied, the position of the photo amongst other photos within the post.
        /// </summary>
        public int? Position { get; set; }
        /// <summary>
        /// References the user who owns the photo. 
        /// Required as for a time photos don't need to be associated with a post and we still need to perform access-control against the photo.
        /// </summary>
        [Required]
        public string UserId { get; set; }
        /// <summary>
        /// The bit length of the photo
        /// </summary>
        [Required]
        public long Length { get; set; }
        /// <summary>
        /// The Mime type of the content in the photo, i.e. image/jpeg.
        /// </summary>
        [Required]
        public string ContentType { get; set; }
        /// <summary>
        /// The width in pixels of the photo.
        /// </summary>
        [Required]
        public int Width { get; set; }
        /// <summary>
        /// The width in pixels of the photo.
        /// </summary>
        [Required]
        public int Height { get; set; }
        /// <summary>
        /// User-posted comments about the photo. Can be nested.
        /// </summary>
        public List<PhotoComment> Comments { get; set; }
        #endregion

        #region legacy accessors
        /// <summary>
        /// Who took the photo?
        /// </summary>
        public string Credit { get; set; }

        /// <summary>
        /// The legacy Galleries id if this was a migrated photo from a Gallery.
        /// </summary>
        public long? LegacyGalleryPhotoId { get; set; }
        #endregion

        #region constructors
        public Photo()
        {
            Created = DateTime.UtcNow;
            Comments = new List<PhotoComment>();
        }
        #endregion

        #region public methods
        public override string ToString()
        {
            return $"{Id}:{PostId}:{UserId}";
        }

        public void SortComments()
        {
            // first sort the parent-less comments
            Comments.Sort();
        }
        #endregion
    }
}