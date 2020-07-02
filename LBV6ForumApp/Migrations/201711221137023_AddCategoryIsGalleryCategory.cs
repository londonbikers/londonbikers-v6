namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddCategoryIsGalleryCategory : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Categories", "IsGalleryCategory", c => c.Boolean(nullable: false));
            Sql("UPDATE dbo.Categories SET IsGalleryCategory = 1 WHERE Name = 'Galleries'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Categories", "IsGalleryCategory");
        }
    }
}
