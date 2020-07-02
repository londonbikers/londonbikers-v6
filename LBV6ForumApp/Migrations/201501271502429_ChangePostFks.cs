namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePostFks : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.Posts", "Forum_Id", "dbo.Forums");
            //DropForeignKey("dbo.Posts", "User_Id", "dbo.AspNetUsers");
            //DropForeignKey("dbo.Posts", "ParentPost_Id", "dbo.Posts");
            //DropIndex("dbo.Posts", new[] { "Forum_Id" });
            //DropIndex("dbo.Posts", new[] { "ParentPost_Id" });
            //DropIndex("dbo.Posts", new[] { "User_Id" });
            //AddColumn("dbo.Posts", "Post_Id", c => c.Long());
            //AlterColumn("dbo.Posts", "User_Id", c => c.String(nullable: false));
            //CreateIndex("dbo.Posts", "Post_Id");
            //AddForeignKey("dbo.Posts", "Post_Id", "dbo.Posts", "Id");
        }
        
        public override void Down()
        {
            //DropForeignKey("dbo.Posts", "Post_Id", "dbo.Posts");
            //DropIndex("dbo.Posts", new[] { "Post_Id" });
            //AlterColumn("dbo.Posts", "User_Id", c => c.String(nullable: false, maxLength: 128));
            //DropColumn("dbo.Posts", "Post_Id");
            //CreateIndex("dbo.Posts", "User_Id");
            //CreateIndex("dbo.Posts", "ParentPost_Id");
            //CreateIndex("dbo.Posts", "Forum_Id");
            //AddForeignKey("dbo.Posts", "ParentPost_Id", "dbo.Posts", "Id");
            //AddForeignKey("dbo.Posts", "User_Id", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            //AddForeignKey("dbo.Posts", "Forum_Id", "dbo.Forums", "Id");
        }
    }
}