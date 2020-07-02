namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserProfileStatIndexes2 : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX IDX_UserProfileRepliesCount ON [dbo].[Posts] ([User_Id],[ParentPost_Id],[Status])");
        }
        
        public override void Down()
        {
            Sql("DROP INDEX IDX_UserProfileRepliesCount");
        }
    }
}