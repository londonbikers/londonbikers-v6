namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixPmHeaderUser : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PrivateMessageUsers", "UserId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PrivateMessageUsers", "UserId", c => c.String());
        }
    }
}
