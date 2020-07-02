namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class SetPhotoRequiredFields : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Photos", "UserId", c => c.String(nullable: false));
            AlterColumn("dbo.Photos", "ContentType", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Photos", "ContentType", c => c.String());
            AlterColumn("dbo.Photos", "UserId", c => c.String());
        }
    }
}