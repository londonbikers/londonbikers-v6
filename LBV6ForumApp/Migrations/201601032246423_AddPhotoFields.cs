namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddPhotoFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Photos", "ContentType", c => c.String());
            AddColumn("dbo.Photos", "Filename", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Photos", "Filename");
            DropColumn("dbo.Photos", "ContentType");
        }
    }
}
