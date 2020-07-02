namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUSerNonMessageNotificationsCount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "NonMessageNotificationsCount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "NonMessageNotificationsCount");
        }
    }
}