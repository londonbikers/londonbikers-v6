namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveCategoryRoles : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ForumAccessRoles", "Category_Id", "dbo.Categories");
            DropIndex("dbo.ForumAccessRoles", new[] { "Category_Id" });
            DropColumn("dbo.ForumAccessRoles", "Category_Id");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ForumAccessRoles", "Category_Id", c => c.Long());
            CreateIndex("dbo.ForumAccessRoles", "Category_Id");
            AddForeignKey("dbo.ForumAccessRoles", "Category_Id", "dbo.Categories", "Id");
        }
    }
}