namespace LBV6.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserTagline : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "Tagline", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Tagline");
        }
    }
}