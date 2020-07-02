namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddForumPostStats : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Forums", "PostCount", c => c.Long(nullable: false));
            AddColumn("dbo.Forums", "LastUpdated", c => c.DateTime());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Forums", "LastUpdated");
            DropColumn("dbo.Forums", "PostCount");
        }
    }
}