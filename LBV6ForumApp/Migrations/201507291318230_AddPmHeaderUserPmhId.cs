namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddPmHeaderUserPmhId : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders");
            DropIndex("dbo.PrivateMessageUsers", new[] { "PrivateMessageHeader_Id" });
            AlterColumn("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id");
            AddForeignKey("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders");
            DropIndex("dbo.PrivateMessageUsers", new[] { "PrivateMessageHeader_Id" });
            AlterColumn("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", c => c.Long());
            CreateIndex("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id");
            AddForeignKey("dbo.PrivateMessageUsers", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders", "Id");
        }
    }
}
