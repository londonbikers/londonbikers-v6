namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangeRoleFk : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ForumAccessRoles", "Forum_Id", "dbo.Forums");
            DropForeignKey("dbo.ForumPostRoles", "Forum_Id", "dbo.Forums");
            DropIndex("dbo.ForumAccessRoles", new[] { "Forum_Id" });
            DropIndex("dbo.ForumPostRoles", new[] { "Forum_Id" });
            AlterColumn("dbo.ForumAccessRoles", "Forum_Id", c => c.Long(nullable: false));
            AlterColumn("dbo.ForumPostRoles", "Forum_Id", c => c.Long(nullable: false));
            CreateIndex("dbo.ForumAccessRoles", "Forum_Id");
            CreateIndex("dbo.ForumPostRoles", "Forum_Id");
            AddForeignKey("dbo.ForumAccessRoles", "Forum_Id", "dbo.Forums", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ForumPostRoles", "Forum_Id", "dbo.Forums", "Id", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ForumPostRoles", "Forum_Id", "dbo.Forums");
            DropForeignKey("dbo.ForumAccessRoles", "Forum_Id", "dbo.Forums");
            DropIndex("dbo.ForumPostRoles", new[] { "Forum_Id" });
            DropIndex("dbo.ForumAccessRoles", new[] { "Forum_Id" });
            AlterColumn("dbo.ForumPostRoles", "Forum_Id", c => c.Long());
            AlterColumn("dbo.ForumAccessRoles", "Forum_Id", c => c.Long());
            CreateIndex("dbo.ForumPostRoles", "Forum_Id");
            CreateIndex("dbo.ForumAccessRoles", "Forum_Id");
            AddForeignKey("dbo.ForumPostRoles", "Forum_Id", "dbo.Forums", "Id");
            AddForeignKey("dbo.ForumAccessRoles", "Forum_Id", "dbo.Forums", "Id");
        }
    }
}