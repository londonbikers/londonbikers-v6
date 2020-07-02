namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserLastVisit : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "LastVisit", c => c.DateTime(nullable: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "LastVisit");
        }
    }
}
