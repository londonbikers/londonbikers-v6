namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddNotificationCommentId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Notifications", "CommentId", c => c.Long());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Notifications", "CommentId");
        }
    }
}
