namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class userswapout : DbMigration
    {
        public override void Up()
        {
            //CreateTable(
            //    "dbo.AspNetUsers",
            //    c => new
            //        {
            //            Id = c.String(nullable: false, maxLength: 128),
            //            Email = c.String(),
            //            UserName = c.String(),
            //        })
            //    .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.Posts", "User_Id", c => c.String(nullable: false, maxLength: 128));
            CreateIndex("dbo.Posts", "User_Id");
            AddForeignKey("dbo.Posts", "User_Id", "dbo.AspNetUsers", "Id", cascadeDelete: true);
            DropColumn("dbo.Posts", "UserId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Posts", "UserId", c => c.String(nullable: false));
            DropForeignKey("dbo.Posts", "User_Id", "dbo.AspNetUsers");
            DropIndex("dbo.Posts", new[] { "User_Id" });
            DropColumn("dbo.Posts", "User_Id");
            //DropTable("dbo.AspNetUsers");
        }
    }
}
