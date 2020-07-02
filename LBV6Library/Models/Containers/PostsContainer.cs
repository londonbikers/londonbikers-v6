using System.Collections.Generic;
using LBV6Library.Interfaces;

namespace LBV6Library.Models.Containers
{
    public class PostsContainer: INotCachable
    {
        public List<Post> Posts { get; set; }
        public List<TopicSeenBy> SeenBys { get; set; }
        public long TotalPosts { get; set; }

        public PostsContainer()
        {
            Posts = new List<Post>();
            SeenBys = new List<TopicSeenBy>();
        }
    }
}