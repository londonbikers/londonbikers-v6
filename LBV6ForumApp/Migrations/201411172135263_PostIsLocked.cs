namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostIsLocked : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "IsLocked", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "IsLocked");
        }
    }
}
