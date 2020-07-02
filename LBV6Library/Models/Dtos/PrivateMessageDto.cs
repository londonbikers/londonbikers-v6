using System;
using System.Collections.Generic;

namespace LBV6Library.Models.Dtos
{
    public class PrivateMessageDto
    {
        public long Id { get; set; }
        public long PrivateMessageHeaderId { get; set; }
        public DateTime Created { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string ProfileFileStoreId { get; set; }
        public string Content { get; set; }
        public List<PrivateMessageReadByDto> ReadBy { get; set; }
        public List<PostAttachmentDto> Photos { get; set; }
        public string Type { get; set; }

        public PrivateMessageDto()
        {
            Created = DateTime.UtcNow;
            ReadBy = new List<PrivateMessageReadByDto>();
            Photos = new List<PostAttachmentDto>();                
        }
    }
}