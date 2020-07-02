namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PhotoCommentAdjustment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PhotoComments", "PhotoComment_Id", c => c.Long());
            CreateIndex("dbo.PhotoComments", "PhotoComment_Id");
            AddForeignKey("dbo.PhotoComments", "PhotoComment_Id", "dbo.PhotoComments", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PhotoComments", "PhotoComment_Id", "dbo.PhotoComments");
            DropIndex("dbo.PhotoComments", new[] { "PhotoComment_Id" });
            DropColumn("dbo.PhotoComments", "PhotoComment_Id");
        }
    }
}