namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPhotoCommentCreated : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PhotoComments", "Created", c => c.DateTime(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PhotoComments", "Created");
        }
    }
}
