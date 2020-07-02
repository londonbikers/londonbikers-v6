namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PrivateMessageHeaderUsers : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PrivateMessageUsers",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.String(),
                        PrivateMessageHeader_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PrivateMessageHeaders", t => t.PrivateMessageHeader_Id)
                .Index(t => t.PrivateMessageHeader_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders");
            DropIndex("dbo.PrivateMessageUsers", new[] { "PrivateMessageHeader_Id" });
            DropTable("dbo.PrivateMessageUsers");
        }
    }
}