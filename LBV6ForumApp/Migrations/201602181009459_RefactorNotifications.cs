namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RefactorNotifications : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        NotificationSubscriptionId = c.Long(nullable: true),
                        ContentId = c.Long(nullable: true),
                        ScenarioType = c.Int(nullable: true),
                        UserId = c.String(nullable: true),
                        Created = c.DateTime(nullable: false),
                        Occurances = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            DropColumn("dbo.NotificationSubscriptions", "MinimumOccurrenceInteval");
            DropColumn("dbo.NotificationSubscriptions", "LastNotificationSent");
            DropColumn("dbo.NotificationSubscriptions", "UnnotifiedOccurrences");
            DropColumn("dbo.PrivateMessageHeaders", "LastNotificationSent");
            DropColumn("dbo.PrivateMessageHeaders", "UnnotifiedOccurrences");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PrivateMessageHeaders", "UnnotifiedOccurrences", c => c.Int());
            AddColumn("dbo.PrivateMessageHeaders", "LastNotificationSent", c => c.DateTime());
            AddColumn("dbo.NotificationSubscriptions", "UnnotifiedOccurrences", c => c.Int());
            AddColumn("dbo.NotificationSubscriptions", "LastNotificationSent", c => c.DateTime());
            AddColumn("dbo.NotificationSubscriptions", "MinimumOccurrenceInteval", c => c.Time(precision: 7));
            DropTable("dbo.Notifications");
        }
    }
}
