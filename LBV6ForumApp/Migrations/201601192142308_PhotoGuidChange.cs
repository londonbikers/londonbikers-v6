namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PhotoGuidChange : DbMigration
    {
        public override void Up()
        {
            Sql("DELETE FROM dbo.Photos");
            Sql("UPDATE dbo.Posts SET [Status] = 1 WHERE Content IS NULL AND [Status] = 0");

            DropPrimaryKey("dbo.Photos");
            DropColumn("dbo.Photos", "Id");
            DropColumn("dbo.Photos", "Filename");

            AddColumn("dbo.Photos", "Id", c => c.Guid(nullable: false, identity: true));
            AddPrimaryKey("dbo.Photos", "Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Photos", "Filename", c => c.String());
            DropPrimaryKey("dbo.Photos");
            AlterColumn("dbo.Photos", "Id", c => c.Long(nullable: false, identity: true));
            AddPrimaryKey("dbo.Photos", "Id");
        }
    }
}
