namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostReplyCount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "ReplyCount", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "ReplyCount");
        }
    }
}