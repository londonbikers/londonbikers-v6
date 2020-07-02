namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class RemovePmSubject : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.PrivateMessageHeaders", "Subject");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PrivateMessageHeaders", "Subject", c => c.String(nullable: false));
        }
    }
}
