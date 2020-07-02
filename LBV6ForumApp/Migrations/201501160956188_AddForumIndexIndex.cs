namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddForumIndexIndex : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_ForumIndex]
                ON [dbo].[Posts] ([Forum_Id],[Status])
                INCLUDE ([Id],[Created],[IsSticky],[LastReplyCreated])");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Posts", "IDX_ForumIndex");
        }
    }
}