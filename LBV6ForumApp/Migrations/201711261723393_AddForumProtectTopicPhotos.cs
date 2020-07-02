namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddForumProtectTopicPhotos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Forums", "ProtectTopicPhotos", c => c.Boolean(nullable: false));
            Sql("UPDATE dbo.Forums SET ProtectTopicPhotos = 1 WHERE [Name] = 'Racing'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Forums", "ProtectTopicPhotos");
        }
    }
}
