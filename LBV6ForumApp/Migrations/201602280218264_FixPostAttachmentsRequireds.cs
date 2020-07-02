namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixPostAttachmentsRequireds : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.PostAttachments", "Filename", c => c.String(nullable: false));
            AlterColumn("dbo.PostAttachments", "ContentType", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PostAttachments", "ContentType", c => c.String());
            AlterColumn("dbo.PostAttachments", "Filename", c => c.String());
        }
    }
}
