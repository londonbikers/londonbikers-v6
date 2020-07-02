namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserCoverDimensions : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "CoverPhotoWidth", c => c.Int());
            AddColumn("dbo.AspNetUsers", "CoverPhotoHeight", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "CoverPhotoHeight");
            DropColumn("dbo.AspNetUsers", "CoverPhotoWidth");
        }
    }
}
