namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class PmHeaderCreated : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessageHeaders", "Created", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessageHeaders", "Created");
        }
    }
}
