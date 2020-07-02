namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class LastReplyCreated : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "LastReplyCreated", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "LastReplyCreated");
        }
    }
}