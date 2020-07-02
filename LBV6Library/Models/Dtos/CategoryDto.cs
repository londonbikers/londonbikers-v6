using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class CategoryDto
    {
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Order { get; set; }
        public bool IsGalleryCategory { get; set; }
        public List<ForumDto> Forums { get; set; }

        public CategoryDto()
        {
            Created = DateTime.UtcNow;
            Forums = new List<ForumDto>();
        }
    }
}