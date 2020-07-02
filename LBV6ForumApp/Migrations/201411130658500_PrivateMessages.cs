namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PrivateMessages : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PrivateMessageHeaders",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Subject = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.PrivateMessages",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.String(nullable: false),
                        Created = c.DateTime(nullable: false),
                        Content = c.String(nullable: false),
                        PrivateMessageHeader_Id = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PrivateMessageHeaders", t => t.PrivateMessageHeader_Id, cascadeDelete: true)
                .Index(t => t.PrivateMessageHeader_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrivateMessages", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders");
            DropIndex("dbo.PrivateMessages", new[] { "PrivateMessageHeader_Id" });
            DropTable("dbo.PrivateMessages");
            DropTable("dbo.PrivateMessageHeaders");
        }
    }
}