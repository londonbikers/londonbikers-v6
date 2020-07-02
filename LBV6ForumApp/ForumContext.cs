using LBV6Library.Models;
using System.Data.Entity;

namespace LBV6ForumApp
{
    public class ForumContext : DbContext
    {
        #region constructors
        public ForumContext() : base("DefaultConnection")
        {
        }
        #endregion

        public DbSet<Category> Categories { get; set; }
        public DbSet<Forum> Forums { get; set; }
        public DbSet<ForumPostRole> ForumPostRoles { get; set; }
        public DbSet<ForumAccessRole> ForumAccessRoles { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<PostAttachment> PostAttachments { get; set; }
        public DbSet<PostModerationHistoryItem> PostModerationHistoryItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserClaim> UserClaims { get; set; }
        public DbSet<UserLogin> UserLogins { get; set; }
        public DbSet<PrivateMessage> PrivateMessages { get; set; }
        public DbSet<PrivateMessageReadBy> PrivateMessageReadBys { get; set; }
        public DbSet<PrivateMessageHeader> PrivateMessageHeaders { get; set; }
        public DbSet<PrivateMessageHeaderUser> PrivateMessageHeaderUsers { get; set; }
        public DbSet<PrivateMessageAttachment> PrivateMessageAttachments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationSubscription> NotificationSubscriptions { get; set; }
        public DbSet<TopicSeenBy> TopicSeenBys { get; set; }
        public DbSet<TopicView> TopicViews { get; set; }
        public DbSet<Photo> Photos { get; set; }
        public DbSet<PhotoComment> PhotoComments { get; set; }
    }
}