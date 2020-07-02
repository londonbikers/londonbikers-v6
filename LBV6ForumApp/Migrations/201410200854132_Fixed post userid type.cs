namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class Fixedpostuseridtype : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Posts", "UserId", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Posts", "UserId", c => c.Int(nullable: false));
        }
    }
}