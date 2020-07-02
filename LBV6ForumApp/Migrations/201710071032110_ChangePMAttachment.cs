namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePmAttachment : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessageAttachments", "FilestoreId", c => c.Guid());
            AddColumn("dbo.PrivateMessageAttachments", "Width", c => c.Int());
            AddColumn("dbo.PrivateMessageAttachments", "Height", c => c.Int());
            DropColumn("dbo.PrivateMessageAttachments", "FileId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.PrivateMessageAttachments", "FileId", c => c.Guid());
            DropColumn("dbo.PrivateMessageAttachments", "Height");
            DropColumn("dbo.PrivateMessageAttachments", "Width");
            DropColumn("dbo.PrivateMessageAttachments", "FilestoreId");
        }
    }
}
