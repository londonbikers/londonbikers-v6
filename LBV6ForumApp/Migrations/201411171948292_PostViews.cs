namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostViews : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "Views", c => c.Long(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "Views");
        }
    }
}
