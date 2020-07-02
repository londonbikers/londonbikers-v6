namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MoreModeration : DbMigration
    {
        public override void Up()
        {
            RenameTable(name: "dbo.ModerationHistoryItems", newName: "PostModerationHistoryItems");
        }
        
        public override void Down()
        {
            RenameTable(name: "dbo.PostModerationHistoryItems", newName: "ModerationHistoryItems");
        }
    }
}