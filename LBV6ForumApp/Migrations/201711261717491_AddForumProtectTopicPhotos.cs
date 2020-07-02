namespace LBV6ForumApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddForumProtectTopicPhotos : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Forums", "ProtectTopicPhotos", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Forums", "ProtectTopicPhotos");
        }
    }
}
