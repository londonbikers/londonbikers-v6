namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePhotoLegacyGalleryImageIdType : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Photos", "LegacyGalleryPhotoId", c => c.Long());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Photos", "LegacyGalleryPhotoId", c => c.Int());
        }
    }
}
