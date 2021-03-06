namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class MakePhotoDimensionsRequired : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Photos", "Width", c => c.Int(nullable: false));
            AlterColumn("dbo.Photos", "Height", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Photos", "Height", c => c.Int());
            AlterColumn("dbo.Photos", "Width", c => c.Int());
        }
    }
}