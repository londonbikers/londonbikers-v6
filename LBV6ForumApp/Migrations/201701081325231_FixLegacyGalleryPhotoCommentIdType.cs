namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixLegacyGalleryPhotoCommentIdType : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PhotoComments", "LegacyCommentId", c => c.Long());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PhotoComments", "LegacyCommentId", c => c.Int());
        }
    }
}
