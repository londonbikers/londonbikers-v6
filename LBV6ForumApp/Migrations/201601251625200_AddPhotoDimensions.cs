namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotoDimensions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Photos", "Width", c => c.Int(nullable: true));
            AddColumn("dbo.Photos", "Height", c => c.Int(nullable: true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "Height");
            DropColumn("dbo.Photos", "Width");
        }
    }
}