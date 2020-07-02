using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class ForumExtendedDto : ForumDto
    {
        public List<ForumRoleDto> AccessRoles { get; set; }
        public List<ForumRoleDto> PostRoles { get; set; }

        public ForumExtendedDto()
        {
            AccessRoles = new List<ForumRoleDto>();
            PostRoles = new List<ForumRoleDto>();
        }
    }
}