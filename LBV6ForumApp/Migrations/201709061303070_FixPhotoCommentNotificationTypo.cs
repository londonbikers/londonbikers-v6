namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixPhotoCommentNotificationTypo : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "NewPhotoCommentNotifications", c => c.Boolean(nullable: false));

            // set initial value
            Sql("UPDATE dbo.AspNetUsers SET NewPhotoCommentNotifications = 1");

            DropColumn("dbo.AspNetUsers", "NewPhotoCommentNotification");
        }
        
        public override void Down()
        {
            AddColumn("dbo.AspNetUsers", "NewPhotoCommentNotification", c => c.Boolean(nullable: false));
            DropColumn("dbo.AspNetUsers", "NewPhotoCommentNotifications");
        }
    }
}