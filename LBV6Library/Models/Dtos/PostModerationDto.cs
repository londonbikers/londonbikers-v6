using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LBV6Library.Models.Dtos
{
    public class PostModerationDto
    {
        public long PostId { get; set; }
        public long DestinationForumId { get; set; }
        public string Reason { get; set; }

        #region validation
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PostId < 1)
                yield return new ValidationResult("A post id is required.", new[] { "PostId" });

            if (string.IsNullOrEmpty(Reason))
                yield return new ValidationResult("A reason is required.", new[] { "Reason" });

            if (string.IsNullOrWhiteSpace(Reason))
                yield return new ValidationResult("A reason is required that is more than just whitespace.", new[] { "Reason" });
        }
        #endregion
    }
}