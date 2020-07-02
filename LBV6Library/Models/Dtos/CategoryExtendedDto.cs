using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class CategoryExtendedDto : CategoryDto
    {
        public new List<ForumExtendedDto> Forums { get; set; }

        public CategoryExtendedDto()
        {
            Forums = new List<ForumExtendedDto>();
        }
    }
}