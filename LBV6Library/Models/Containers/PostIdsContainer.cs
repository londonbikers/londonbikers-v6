using System.Collections.Generic;

namespace LBV6Library.Models.Containers
{
    public class PostIdsContainer
    {
        public List<long> PostIds { get; set; }
        public long TotalPosts { get; set; }

        public PostIdsContainer()
        {
            PostIds = new List<long>();
        }
    }
}
