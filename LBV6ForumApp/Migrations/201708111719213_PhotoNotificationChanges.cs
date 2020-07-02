namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PhotoNotificationChanges : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Notifications", "ContentGuid", c => c.Guid());
            AddColumn("dbo.AspNetUsers", "NewPhotoCommentNotification", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "NewPhotoCommentNotification");
            DropColumn("dbo.Notifications", "ContentGuid");
        }
    }
}