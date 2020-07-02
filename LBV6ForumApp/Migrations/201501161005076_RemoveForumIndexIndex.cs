namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveForumIndexIndex : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.Posts", "IDX_ForumIndex");
        }
        
        public override void Down()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_ForumIndex]
                ON [dbo].[Posts] ([Forum_Id],[Status])
                INCLUDE ([Id],[Created],[IsSticky],[LastReplyCreated])");
        }
    }
}