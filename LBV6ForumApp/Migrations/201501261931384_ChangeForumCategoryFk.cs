namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangeForumCategoryFk : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Forums", "Category_Id", "dbo.Categories");
            DropIndex("dbo.Forums", new[] { "Category_Id" });
            AlterColumn("dbo.Forums", "Category_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.Forums", "Category_Id");
            AddForeignKey("dbo.Forums", "Category_Id", "dbo.Categories", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Forums", "Category_Id", "dbo.Categories");
            DropIndex("dbo.Forums", new[] { "Category_Id" });
            AlterColumn("dbo.Forums", "Category_Id", c => c.Long());
            CreateIndex("dbo.Forums", "Category_Id");
            AddForeignKey("dbo.Forums", "Category_Id", "dbo.Categories", "Id");
        }
    }
}