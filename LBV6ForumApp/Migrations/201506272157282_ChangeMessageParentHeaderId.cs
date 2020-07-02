namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangeMessageParentHeaderId : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.PrivateMessages", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders");
            //DropIndex("dbo.PrivateMessages", new[] { "PrivateMessageHeader_Id" });
        }
        
        public override void Down()
        {
            //CreateIndex("dbo.PrivateMessages", "PrivateMessageHeader_Id");
            //AddForeignKey("dbo.PrivateMessages", "PrivateMessageHeader_Id", "dbo.PrivateMessageHeaders", "Id", cascadeDelete: true);
        }
    }
}