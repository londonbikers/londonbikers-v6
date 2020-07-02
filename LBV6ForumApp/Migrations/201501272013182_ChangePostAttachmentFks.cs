namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePostAttachmentFks : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PostAttachments", "Post_Id", "dbo.Posts");
            DropIndex("dbo.PostAttachments", new[] { "Post_Id" });
            AlterColumn("dbo.PostAttachments", "Post_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.PostAttachments", "Post_Id");
            AddForeignKey("dbo.PostAttachments", "Post_Id", "dbo.Posts", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PostAttachments", "Post_Id", "dbo.Posts");
            DropIndex("dbo.PostAttachments", new[] { "Post_Id" });
            AlterColumn("dbo.PostAttachments", "Post_Id", c => c.Long());
            CreateIndex("dbo.PostAttachments", "Post_Id");
            AddForeignKey("dbo.PostAttachments", "Post_Id", "dbo.Posts", "Id");
        }
    }
}