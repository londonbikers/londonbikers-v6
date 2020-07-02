namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class RemoveEditedBy : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_GetTopic] ON [dbo].[Posts]
(
	[ParentPost_Id] ASC
)
INCLUDE ( 	[Id],
	[Created],
	[Subject],
	[Content],
	[IsSticky],
	[UpVotes],
	[DownVotes],
	[IsAnswer],
	[Forum_Id],
	[User_Id],
	[Views],
	[LegacyPostId],
	[EditedOn],
	[Status],
	[LastReplyCreated]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = ON, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO");
            DropColumn("dbo.Posts", "EditedByUserId");
        }
        
        public override void Down()
        {

            AddColumn("dbo.Posts", "EditedByUserId", c => c.String());
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_GetTopic] ON [dbo].[Posts]
(
	[ParentPost_Id] ASC
)
INCLUDE ( 	[Id],
	[Created],
	[Subject],
	[Content],
	[IsSticky],
	[UpVotes],
	[DownVotes],
	[IsAnswer],
	[Forum_Id],
	[User_Id],
	[Views],
	[LegacyPostId],
	[EditedByUserId],
	[EditedOn],
	[Status],
	[LastReplyCreated]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO");
        }
    }
}