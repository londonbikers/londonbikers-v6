namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class ResetPostIndexes : DbMigration
    {
        public override void Up()
        {
            DropIndex("Posts", "IDX_Created");
            DropIndex("Posts", "IDX_ForumHeadersOrdering");
            DropIndex("Posts", "IX_Forum_Id");
            DropIndex("Posts", "IX_ParentPost_Id");
            DropIndex("Posts", "IX_User_Id");

            Sql(@"CREATE NONCLUSTERED INDEX [IDX_PopularTopics]
                ON [dbo].[Posts] ([Created])
                INCLUDE ([ParentPost_Id])
                GO");
        }
        
        public override void Down()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_Created] ON [dbo].[Posts]
                (
	                [Created] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90)
                GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IDX_ForumHeadersOrdering] ON [dbo].[Posts]
                (
	                [IsSticky] DESC,
	                [LastReplyCreated] DESC,
	                [Created] DESC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
                GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IX_Forum_Id] ON [dbo].[Posts]
                (
	                [Forum_Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90)
                GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IX_ParentPost_Id] ON [dbo].[Posts]
                (
	                [ParentPost_Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90)
                GO");

            Sql(@"CREATE NONCLUSTERED INDEX [IX_User_Id] ON [dbo].[Posts]
                (
	                [User_Id] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, FILLFACTOR = 90)
                GO");

            DropIndex("Posts", "IDX_PopularTopics");
        }
    }
}