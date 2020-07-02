namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AlterPmHeader : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessageUsers", "Added", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessageUsers", "Added");
        }
    }
}