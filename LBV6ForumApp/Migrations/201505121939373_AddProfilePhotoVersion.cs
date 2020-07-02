namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProfilePhotoVersion : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "ProfilePhotoVersion", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "ProfilePhotoVersion");
        }
    }
}
