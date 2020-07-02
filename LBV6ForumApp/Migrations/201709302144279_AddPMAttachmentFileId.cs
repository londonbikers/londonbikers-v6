namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class AddPMAttachmentFileId : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessageAttachments", "FileId", c => c.Guid());
        }
        
        public override void Down()
        {
            DropColumn("dbo.PrivateMessageAttachments", "FileId");
        }
    }
}