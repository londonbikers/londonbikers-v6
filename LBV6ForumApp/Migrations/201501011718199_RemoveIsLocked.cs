namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveIsLocked : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.Posts", "IsLocked");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Posts", "IsLocked", c => c.Boolean());
        }
    }
}