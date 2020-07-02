using System;

namespace LBV6Library.Models.Dtos
{
    public class PrivateMessageHeaderUserDto
    {
        public long Id { get; set; }
        public UserProfileLightDto User { get; set; }
        public DateTime Added { get; set; }

        public PrivateMessageHeaderUserDto()
        {
            Added = DateTime.UtcNow;
        }
    }
}