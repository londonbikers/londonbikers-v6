namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PmLegacyId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessages", "LegacyMessageId", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessages", "LegacyMessageId");
        }
    }
}
