namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostNullViews : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Posts", "Views", c => c.Long());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Posts", "Views", c => c.Long(nullable: false));
        }
    }
}
