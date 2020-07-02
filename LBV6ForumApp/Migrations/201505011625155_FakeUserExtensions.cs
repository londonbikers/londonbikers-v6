namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FakeUserExtensions : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.AspNetUsers", "ReceiveNewsletters", c => c.Boolean(nullable: false));
            //AddColumn("dbo.AspNetUsers", "TotalVists", c => c.Int(nullable: false));
            //AddColumn("dbo.AspNetUsers", "Status", c => c.Int(nullable: false));
            //AddColumn("dbo.AspNetUsers", "Tagline", c => c.String());
        }
        
        public override void Down()
        {
            //DropColumn("dbo.AspNetUsers", "Tagline");
            //DropColumn("dbo.AspNetUsers", "Status");
            //DropColumn("dbo.AspNetUsers", "TotalVists");
            //DropColumn("dbo.AspNetUsers", "ReceiveNewsletters");
        }
    }
}