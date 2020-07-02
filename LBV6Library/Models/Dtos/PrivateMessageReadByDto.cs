using System;

namespace LBV6Library.Models.Dtos
{
    public class PrivateMessageReadByDto
    {
        public long Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime When { get; set; }

        public PrivateMessageReadByDto()
        {
        }
    }
}