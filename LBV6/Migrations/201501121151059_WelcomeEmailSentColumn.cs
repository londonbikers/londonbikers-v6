namespace LBV6.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class WelcomeEmailSentColumn : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "WelcomeEmailSent", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "WelcomeEmailSent");
        }
    }
}