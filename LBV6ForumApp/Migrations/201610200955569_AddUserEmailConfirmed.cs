namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserEmailConfirmed : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.AspNetUsers", "EmailConfirmed", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            //DropColumn("dbo.AspNetUsers", "EmailConfirmed");
        }
    }
}
