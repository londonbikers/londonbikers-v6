namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserVerified : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Verified", c => c.Boolean());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Verified");
        }
    }
}
