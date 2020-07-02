namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddCoverPhotoCols : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "CoverPhotoId", c => c.Guid(nullable:true));
            AddColumn("dbo.AspNetUsers", "CoverPhotoContentType", c => c.String(nullable:true));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "CoverPhotoContentType");
            DropColumn("dbo.AspNetUsers", "CoverPhotoId");
        }
    }
}
