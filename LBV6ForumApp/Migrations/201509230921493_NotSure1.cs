namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class NotSure1 : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.AspNetUsers", "LastVisit", c => c.DateTime());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.AspNetUsers", "LastVisit", c => c.DateTime(nullable: false));
        }
    }
}
