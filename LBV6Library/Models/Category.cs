using LBV6Library.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Models
{
    [Table("Categories")]
    public class Category : ICommon, INotCachable
    {
        #region accessors
        public long Id { get; set; }
        public DateTime Created { get; set; }
        [Required]
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        /// <summary>
        /// When set to true, posts created in any forum that are under this category will be marked as Gallery posts.
        /// </summary>
        public bool IsGalleryCategory { get; set; }
        public virtual List<Forum> Forums { get; set; }
        #endregion

        #region constructors
        public Category()
        {
            Created = DateTime.UtcNow;
        }
        #endregion

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name))
                yield return new ValidationResult("Category requires a name.", new[] { "Name" });
        }
    }
}