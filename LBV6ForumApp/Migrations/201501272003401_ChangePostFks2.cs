namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePostFks2 : DbMigration
    {
        public override void Up()
        {
            //DropColumn("dbo.Posts", "ParentPost_Id");
            //RenameColumn(table: "dbo.Posts", name: "Post_Id", newName: "ParentPost_Id");
            //RenameIndex(table: "dbo.Posts", name: "IX_Post_Id", newName: "IX_ParentPost_Id");
        }
        
        public override void Down()
        {
            //RenameIndex(table: "dbo.Posts", name: "IX_ParentPost_Id", newName: "IX_Post_Id");
            //RenameColumn(table: "dbo.Posts", name: "ParentPost_Id", newName: "Post_Id");
            //AddColumn("dbo.Posts", "ParentPost_Id", c => c.Long());
        }
    }
}