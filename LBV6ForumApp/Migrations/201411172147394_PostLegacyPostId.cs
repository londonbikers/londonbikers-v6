namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostLegacyPostId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "LegacyPostId", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "LegacyPostId");
        }
    }
}