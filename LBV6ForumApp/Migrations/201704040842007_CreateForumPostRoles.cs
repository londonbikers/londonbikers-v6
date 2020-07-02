namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateForumPostRoles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ForumPostRoles",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Role = c.String(),
                        Forum_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Forums", t => t.Forum_Id)
                .Index(t => t.Forum_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ForumPostRoles", "Forum_Id", "dbo.Forums");
            DropIndex("dbo.ForumPostRoles", new[] { "Forum_Id" });
            DropTable("dbo.ForumPostRoles");
        }
    }
}
