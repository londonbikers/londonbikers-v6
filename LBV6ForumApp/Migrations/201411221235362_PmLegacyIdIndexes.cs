namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class PmLegacyIdIndexes : DbMigration
    {
        public override void Up()
        {
            CreateIndex("dbo.PrivateMessageAttachments", "LegacyId", false, "IDX_LegacyId");
            CreateIndex("dbo.PostAttachments", "LegacyId", false, "IDX_LegacyId");
        }

        public override void Down()
        {
            DropIndex("dbo.PrivateMessageAttachments", "IDX_LegacyId");
            DropIndex("dbo.PostAttachments", "IDX_LegacyId");
        }
    }
}