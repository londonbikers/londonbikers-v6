namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MakePostContentOptional : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Posts", "Content", c => c.String(nullable: true));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Posts", "Content", c => c.String(nullable: false));
        }
    }
}