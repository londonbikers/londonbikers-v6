namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixingPhotoCommentChildCommentFk : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.PhotoComments", "ParentCommentId");
            RenameColumn(table: "dbo.PhotoComments", name: "PhotoComment_Id", newName: "ParentCommentId");
            RenameIndex(table: "dbo.PhotoComments", name: "IX_PhotoComment_Id", newName: "IX_ParentCommentId");
        }
        
        public override void Down()
        {
            RenameIndex(table: "dbo.PhotoComments", name: "IX_ParentCommentId", newName: "IX_PhotoComment_Id");
            RenameColumn(table: "dbo.PhotoComments", name: "ParentCommentId", newName: "PhotoComment_Id");
            AddColumn("dbo.PhotoComments", "ParentCommentId", c => c.Long());
        }
    }
}
