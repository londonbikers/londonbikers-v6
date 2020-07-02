namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Categories",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        Name = c.String(nullable: false),
                        Order = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Forums",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        Name = c.String(nullable: false),
                        Order = c.Int(nullable: false),
                        Category_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categories", t => t.Category_Id)
                .Index(t => t.Category_Id);
            
            CreateTable(
                "dbo.Posts",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        Created = c.DateTime(nullable: false),
                        UserId = c.Int(nullable: false),
                        Subject = c.String(),
                        Content = c.String(nullable: false),
                        IsSticky = c.Boolean(nullable: false),
                        UpVotes = c.Int(nullable: false),
                        DownVotes = c.Int(nullable: false),
                        IsAnswer = c.Boolean(nullable: false),
                        Forum_Id = c.Long(),
                        ParentPost_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Forums", t => t.Forum_Id)
                .ForeignKey("dbo.Posts", t => t.ParentPost_Id)
                .Index(t => t.Forum_Id)
                .Index(t => t.ParentPost_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Posts", "ParentPost_Id", "dbo.Posts");
            DropForeignKey("dbo.Posts", "Forum_Id", "dbo.Forums");
            DropForeignKey("dbo.Forums", "Category_Id", "dbo.Categories");
            DropIndex("dbo.Posts", new[] { "ParentPost_Id" });
            DropIndex("dbo.Posts", new[] { "Forum_Id" });
            DropIndex("dbo.Forums", new[] { "Category_Id" });
            DropTable("dbo.Posts");
            DropTable("dbo.Forums");
            DropTable("dbo.Categories");
        }
    }
}
