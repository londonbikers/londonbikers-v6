namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveMinimumOccurrenceIntevalCol : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.PrivateMessageHeaders", "MinimumOccurrenceInteval");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PrivateMessageHeaders", "MinimumOccurrenceInteval", c => c.Time(precision: 7));
        }
    }
}