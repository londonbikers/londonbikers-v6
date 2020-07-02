namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class ChangePmHeaderDatesAndUsers : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PrivateMessageReadBys", "PrivateMessage_Id", "dbo.PrivateMessages");
            DropIndex("dbo.PrivateMessageReadBys", new[] { "PrivateMessage_Id" });
            AddColumn("dbo.PrivateMessageHeaders", "LastMessageCreated", c => c.DateTime());
            AddColumn("dbo.PrivateMessageUsers", "HasUnreadMessages", c => c.Boolean(nullable: false));
            AlterColumn("dbo.PrivateMessageReadBys", "PrivateMessage_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.PrivateMessageReadBys", "PrivateMessage_Id");
            AddForeignKey("dbo.PrivateMessageReadBys", "PrivateMessage_Id", "dbo.PrivateMessages", "Id", cascadeDelete: true);
            DropColumn("dbo.PrivateMessageHeaders", "Created");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PrivateMessageHeaders", "Created", c => c.DateTime(nullable: false));
            DropForeignKey("dbo.PrivateMessageReadBys", "PrivateMessage_Id", "dbo.PrivateMessages");
            DropIndex("dbo.PrivateMessageReadBys", new[] { "PrivateMessage_Id" });
            AlterColumn("dbo.PrivateMessageReadBys", "PrivateMessage_Id", c => c.Long());
            DropColumn("dbo.PrivateMessageUsers", "HasUnreadMessages");
            DropColumn("dbo.PrivateMessageHeaders", "LastMessageCreated");
            CreateIndex("dbo.PrivateMessageReadBys", "PrivateMessage_Id");
            AddForeignKey("dbo.PrivateMessageReadBys", "PrivateMessage_Id", "dbo.PrivateMessages", "Id");
        }
    }
}
