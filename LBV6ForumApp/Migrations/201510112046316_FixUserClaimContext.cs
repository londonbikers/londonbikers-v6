namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class FixUserClaimContext : DbMigration
    {
        public override void Up()
        {
            //RenameTable(name: "dbo.UserClaims", newName: "AspNetUserClaims");
        }
        
        public override void Down()
        {
            //RenameTable(name: "dbo.AspNetUserClaims", newName: "UserClaims");
        }
    }
}
