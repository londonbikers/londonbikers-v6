using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class PhotoDto
    {
        public long PostId { get; set; }
        public Guid Id { get; set; }
        public string FilestoreId { get; set; }
        public int? Position { get; set; }
        public DateTime Created { get; set; }
        public string Caption { get; set; }
        public string Credit { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public List<PhotoCommentDto> Comments { get; set; }

        public PhotoDto()
        {
            Comments = new List<PhotoCommentDto>();
        }
    }
}