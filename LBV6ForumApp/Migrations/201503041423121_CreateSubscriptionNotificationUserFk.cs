namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateSubscriptionNotificationUserFk : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.NotificationSubscriptions", "UserId", c => c.String(nullable: false, maxLength:128));
            AddForeignKey("dbo.NotificationSubscriptions", "UserId", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.NotificationSubscriptions", "UserId", "dbo.AspNetUsers");
            AlterColumn("dbo.NotificationSubscriptions", "UserId", c => c.String(nullable: false));
        }
    }
}