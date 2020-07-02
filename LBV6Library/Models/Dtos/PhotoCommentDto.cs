using System;

namespace LBV6Library.Models.Dtos
{
    public class PhotoCommentDto
    {
        public Guid PhotoId { get; set; }
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public string Text { get; set; }

        // todo: parent/child comments to come...
    }
}