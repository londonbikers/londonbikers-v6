namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ChangePmhiFks : DbMigration
    {
        public override void Up()
        {
            //DropForeignKey("dbo.PostModerationHistoryItems", "Moderator_Id", "dbo.AspNetUsers");
            //DropIndex("dbo.PostModerationHistoryItems", new[] { "Moderator_Id" });
            //AlterColumn("dbo.PostModerationHistoryItems", "Moderator_Id", c => c.String(nullable: false));
        }
        
        public override void Down()
        {
            //AlterColumn("dbo.PostModerationHistoryItems", "Moderator_Id", c => c.String(nullable: false, maxLength: 128));
            //CreateIndex("dbo.PostModerationHistoryItems", "Moderator_Id");
            //AddForeignKey("dbo.PostModerationHistoryItems", "Moderator_Id", "dbo.AspNetUsers", "Id", cascadeDelete: true);
        }
    }
}