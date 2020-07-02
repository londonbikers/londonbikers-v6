namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotoLegacyId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Photos", "LegacyGalleryPhotoId", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "LegacyGalleryPhotoId");
        }
    }
}