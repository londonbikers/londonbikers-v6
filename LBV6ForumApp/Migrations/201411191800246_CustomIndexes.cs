namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CustomIndexes : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.Categories", "Order", false, "IDX_Order");
            CreateIndex("dbo.Posts", "Created", false, "IDX_Created");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Categories", "IDX_Order");
            DropIndex("dbo.Posts", "IDX_Created");
        }
    }
}