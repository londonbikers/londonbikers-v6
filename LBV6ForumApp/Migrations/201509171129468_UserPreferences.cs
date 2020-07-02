namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserPreferences : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "NewTopicNotifications", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.AspNetUsers", "NewReplyNotifications", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.AspNetUsers", "NewMessageNotifications", c => c.Boolean(nullable: false, defaultValue: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "NewMessageNotifications");
            DropColumn("dbo.AspNetUsers", "NewReplyNotifications");
            DropColumn("dbo.AspNetUsers", "NewTopicNotifications");
        }
    }
}