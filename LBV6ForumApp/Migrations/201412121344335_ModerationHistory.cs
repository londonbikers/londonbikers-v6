namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ModerationHistory : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ModerationHistoryItems",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Type = c.Int(nullable: false),
                        Justification = c.String(nullable: false),
                        Created = c.DateTime(nullable: false),
                        Moderator_Id = c.String(nullable: false, maxLength: 128),
                        Post_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.Moderator_Id, cascadeDelete: true)
                .ForeignKey("dbo.Posts", t => t.Post_Id, cascadeDelete: false)
                .Index(t => t.Moderator_Id)
                .Index(t => t.Post_Id);
            
            AddColumn("dbo.Posts", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ModerationHistoryItems", "Post_Id", "dbo.Posts");
            DropForeignKey("dbo.ModerationHistoryItems", "Moderator_Id", "dbo.AspNetUsers");
            DropIndex("dbo.ModerationHistoryItems", new[] { "Post_Id" });
            DropIndex("dbo.ModerationHistoryItems", new[] { "Moderator_Id" });
            DropColumn("dbo.Posts", "Status");
            DropTable("dbo.ModerationHistoryItems");
        }
    }
}