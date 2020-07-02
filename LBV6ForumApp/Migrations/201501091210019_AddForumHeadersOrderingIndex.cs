namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddForumHeadersOrderingIndex : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_ForumHeadersOrdering] ON [dbo].[Posts]
                (
	                [IsSticky] DESC,
	                [LastReplyCreated] DESC,
	                [Created] DESC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Posts", "IDX_ForumHeadersOrdering");
        }
    }
}