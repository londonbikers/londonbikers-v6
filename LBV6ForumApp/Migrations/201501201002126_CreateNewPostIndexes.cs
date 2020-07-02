namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateNewPostIndexes : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_GetTopic]
ON [dbo].[Posts] ([ParentPost_Id])
INCLUDE ([Id],[Created],[Subject],[Content],[IsSticky],[UpVotes],[DownVotes],[IsAnswer],[Forum_Id],[User_Id],[Views],[LegacyPostId],[EditedByUserId],[EditedOn],[Status],[LastReplyCreated])
GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IDX_GetForumIndexHeaders]
ON [dbo].[Posts] ([Forum_Id],[Status])
INCLUDE ([Id],[Created],[IsSticky],[LastReplyCreated])
GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IDX_GetForumIndexCount]
ON [dbo].[Posts] ([Forum_Id])
GO");
        }
        
        public override void Down()
        {
            DropIndex("Posts", "IDX_GetTopic");
            DropIndex("Posts", "IDX_GetForumIndexHeaders");
            DropIndex("Posts", "IDX_GetForumIndexCount");
        }
    }
}