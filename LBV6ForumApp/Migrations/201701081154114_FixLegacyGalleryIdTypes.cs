namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixLegacyGalleryIdTypes : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Posts", "LegacyGalleryId", c => c.Long());
            AlterColumn("dbo.Posts", "LegacyCommentId", c => c.Long());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Posts", "LegacyCommentId", c => c.Int());
            AlterColumn("dbo.Posts", "LegacyGalleryId", c => c.Int());
        }
    }
}