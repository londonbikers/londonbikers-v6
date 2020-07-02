namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ForumAccessRoles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ForumAccessRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        Category_Id = c.Long(),
                        Forum_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categories", t => t.Category_Id)
                .ForeignKey("dbo.Forums", t => t.Forum_Id)
                .Index(t => t.Category_Id)
                .Index(t => t.Forum_Id);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ForumAccessRoles", "Forum_Id", "dbo.Forums");
            DropForeignKey("dbo.ForumAccessRoles", "Category_Id", "dbo.Categories");
            DropIndex("dbo.ForumAccessRoles", new[] { "Forum_Id" });
            DropIndex("dbo.ForumAccessRoles", new[] { "Category_Id" });
            DropTable("dbo.ForumAccessRoles");
        }
    }
}
