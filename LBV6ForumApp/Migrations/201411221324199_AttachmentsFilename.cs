namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AttachmentsFilename : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PostAttachments", "Filename", c => c.String());
            AddColumn("dbo.PrivateMessageAttachments", "Filename", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessageAttachments", "Filename");
            DropColumn("dbo.PostAttachments", "Filename");
        }
    }
}