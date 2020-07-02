namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixReadByUserId : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PrivateMessageReadBys", "UserId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PrivateMessageReadBys", "UserId", c => c.String());
        }
    }
}
