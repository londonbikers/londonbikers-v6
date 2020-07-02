using System;

namespace LBV6Library.Models.Dtos
{
    public class ForumDto
    {
        public long Id { get; set; }
        public DateTime Created { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public long CategoryId { get; set; }
        public string CategoryName { get; set; }
        public int Order { get; set; }
        public long PostCount { get; set; }
        public DateTime LastUpdated { get; set; }

        public ForumDto()
        {
            Created = DateTime.UtcNow;
        }
    }
}