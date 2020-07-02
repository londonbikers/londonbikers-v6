namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostNullables : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Posts", "IsSticky", c => c.Boolean());
            AlterColumn("dbo.Posts", "UpVotes", c => c.Int());
            AlterColumn("dbo.Posts", "DownVotes", c => c.Int());
            AlterColumn("dbo.Posts", "IsAnswer", c => c.Boolean());
            AlterColumn("dbo.Posts", "IsLocked", c => c.Boolean());
            AlterColumn("dbo.Posts", "EditedOn", c => c.DateTime());
            AlterColumn("dbo.Posts", "LegacyPostId", c => c.Int());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Posts", "LegacyPostId", c => c.Int(nullable: false));
            AlterColumn("dbo.Posts", "EditedOn", c => c.DateTime(nullable: false));
            AlterColumn("dbo.Posts", "IsLocked", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Posts", "IsAnswer", c => c.Boolean(nullable: false));
            AlterColumn("dbo.Posts", "DownVotes", c => c.Int(nullable: false));
            AlterColumn("dbo.Posts", "UpVotes", c => c.Int(nullable: false));
            AlterColumn("dbo.Posts", "IsSticky", c => c.Boolean(nullable: false));
        }
    }
}
