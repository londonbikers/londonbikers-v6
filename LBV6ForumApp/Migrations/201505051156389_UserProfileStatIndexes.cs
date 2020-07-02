namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UserProfileStatIndexes : DbMigration
    {
        public override void Up()
        {
            Sql("CREATE NONCLUSTERED INDEX IDX_UserProfileTopicsCount ON [dbo].[Posts] ([ParentPost_Id],[User_Id],[Status])");
        }
        
        public override void Down()
        {
            Sql("DROP INDEX IDX_UserProfileTopicsCount");
        }
    }
}