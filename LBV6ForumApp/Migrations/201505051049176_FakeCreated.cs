namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FakeCreated : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.AspNetUsers", "Created", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            //DropColumn("dbo.AspNetUsers", "Created");
        }
    }
}