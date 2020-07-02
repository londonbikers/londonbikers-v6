using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class CategorySimpleDto
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<ForumSimpleDto> Forums { get; set; }
        public bool IsGalleryCategory { get; set; }

        public CategorySimpleDto()
        {
            Forums = new List<ForumSimpleDto>();
        }
    }
}