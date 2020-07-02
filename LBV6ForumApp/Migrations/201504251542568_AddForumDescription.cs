namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddForumDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Forums", "Description", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Forums", "Description");
        }
    }
}