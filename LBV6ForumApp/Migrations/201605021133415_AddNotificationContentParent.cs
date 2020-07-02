namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddNotificationContentParent : DbMigration
    {
        public override void Up()
        {
            Sql("TRUNCATE TABLE Notifications");
            AddColumn("dbo.Notifications", "ContentParentId", c => c.Long());
            AddColumn("dbo.Notifications", "Updated", c => c.DateTime(nullable: false));
            DropColumn("dbo.Notifications", "Created");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Notifications", "Created", c => c.DateTime(nullable: false));
            DropColumn("dbo.Notifications", "Updated");
            DropColumn("dbo.Notifications", "ContentParentId");
        }
    }
}