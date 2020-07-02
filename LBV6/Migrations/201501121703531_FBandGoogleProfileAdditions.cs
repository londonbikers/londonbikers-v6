namespace LBV6.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FBandGoogleProfileAdditions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "AgeMin", c => c.Int());
            AddColumn("dbo.AspNetUsers", "AgeMax", c => c.Int());
            AddColumn("dbo.AspNetUsers", "Gender", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "Gender");
            DropColumn("dbo.AspNetUsers", "AgeMax");
            DropColumn("dbo.AspNetUsers", "AgeMin");
        }
    }
}