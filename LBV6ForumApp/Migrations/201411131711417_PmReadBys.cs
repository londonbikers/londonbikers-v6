namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PmReadBys : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.PrivateMessageReadBys",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        UserId = c.String(),
                        When = c.DateTime(nullable: false),
                        PrivateMessage_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PrivateMessages", t => t.PrivateMessage_Id)
                .Index(t => t.PrivateMessage_Id);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrivateMessageReadBys", "PrivateMessage_Id", "dbo.PrivateMessages");
            DropIndex("dbo.PrivateMessageReadBys", new[] { "PrivateMessage_Id" });
            DropTable("dbo.PrivateMessageReadBys");
        }
    }
}