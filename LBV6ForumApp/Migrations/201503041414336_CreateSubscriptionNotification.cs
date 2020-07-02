namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateSubscriptionNotification : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.NotificationSubscriptions",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.String(),
                        Type = c.Int(nullable: false),
                        SubjectId = c.Long(nullable: false),
                        MinimumOccurrenceInteval = c.Time(precision: 7),
                        LastNotificationSent = c.DateTime(),
                        UnnotifiedOccurrences = c.Int(),
                    })
                .PrimaryKey(t => t.Id);
        }
        
        public override void Down()
        {
            DropTable("dbo.NotificationSubscriptions");
        }
    }
}