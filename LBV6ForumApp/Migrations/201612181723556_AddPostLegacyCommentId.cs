namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPostLegacyCommentId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PhotoComments", "LegacyCommentId", c => c.Int());
            AddColumn("dbo.Posts", "LegacyCommentId", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "LegacyCommentId");
            DropColumn("dbo.PhotoComments", "LegacyCommentId");
        }
    }
}