namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PostEditedBy : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Posts", "EditedByUserId", c => c.String());
            AddColumn("dbo.Posts", "EditedOn", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Posts", "EditedOn");
            DropColumn("dbo.Posts", "EditedByUserId");
        }
    }
}
