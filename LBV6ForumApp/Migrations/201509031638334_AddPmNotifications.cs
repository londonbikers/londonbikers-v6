namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPmNotifications : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessageHeaders", "MinimumOccurrenceInteval", c => c.Time(precision: 7));
            AddColumn("dbo.PrivateMessageHeaders", "LastNotificationSent", c => c.DateTime());
            AddColumn("dbo.PrivateMessageHeaders", "UnnotifiedOccurrences", c => c.Int());
            Sql("UPDATE dbo.PrivateMessageHeaders SET MinimumOccurrenceInteval = '00:05:00.0000000'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessageHeaders", "UnnotifiedOccurrences");
            DropColumn("dbo.PrivateMessageHeaders", "LastNotificationSent");
            DropColumn("dbo.PrivateMessageHeaders", "MinimumOccurrenceInteval");
        }
    }
}
