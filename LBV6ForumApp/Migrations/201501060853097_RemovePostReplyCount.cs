namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemovePostReplyCount : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Posts", "ReplyCount");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Posts", "ReplyCount", c => c.Int());
        }
    }
}