namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Attachments : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PostAttachments",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        ContentType = c.String(),
                        Views = c.Long(nullable: false),
                        LegacyId = c.Int(),
                        Post_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Posts", t => t.Post_Id)
                .Index(t => t.Post_Id);
            
            CreateTable(
                "dbo.PrivateMessageAttachments",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        ContentType = c.String(),
                        LegacyId = c.Int(),
                        PrivateMessage_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PrivateMessages", t => t.PrivateMessage_Id)
                .Index(t => t.PrivateMessage_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrivateMessageAttachments", "PrivateMessage_Id", "dbo.PrivateMessages");
            DropForeignKey("dbo.PostAttachments", "Post_Id", "dbo.Posts");
            DropIndex("dbo.PrivateMessageAttachments", new[] { "PrivateMessage_Id" });
            DropIndex("dbo.PostAttachments", new[] { "Post_Id" });
            DropTable("dbo.PrivateMessageAttachments");
            DropTable("dbo.PostAttachments");
        }
    }
}
