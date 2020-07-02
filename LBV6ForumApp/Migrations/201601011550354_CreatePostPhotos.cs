namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreatePostPhotos : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Photos", "PostId");
            AddForeignKey("dbo.Photos", "PostId", "dbo.Posts", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Photos", "PostId", "dbo.Posts");
            DropIndex("dbo.Photos", new[] { "PostId" });
        }
    }
}