namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PhotoGallerySupportr : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PhotoComments",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        PhotoId = c.Guid(nullable: false),
                        ParentCommentId = c.Long(),
                        UserId = c.String(),
                        Text = c.String(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Photos", t => t.PhotoId, cascadeDelete: true)
                .Index(t => t.PhotoId);
            
            AddColumn("dbo.Photos", "Credit", c => c.String());
            AddColumn("dbo.Posts", "LegacyGalleryId", c => c.Int());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhotoComments", "PhotoId", "dbo.Photos");
            DropIndex("dbo.PhotoComments", new[] { "PhotoId" });
            DropColumn("dbo.Posts", "LegacyGalleryId");
            DropColumn("dbo.Photos", "Credit");
            DropTable("dbo.PhotoComments");
        }
    }
}
