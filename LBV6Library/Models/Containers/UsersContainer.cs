using LBV6Library.Interfaces;
using System.Collections.Generic;

namespace LBV6Library.Models.Containers
{
    public class UsersContainer: INotCachable
    {
        public List<User> Users { get; set; }
        public long TotalUsers { get; set; }

        public UsersContainer()
        {
            Users = new List<User>();
        }
    }
}