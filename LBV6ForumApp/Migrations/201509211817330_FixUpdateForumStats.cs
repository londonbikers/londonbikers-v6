namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixUpdateForumStats : DbMigration
    {
        public override void Up()
        {
            Sql(@"ALTER PROCEDURE GetForumStats
	@ForumId bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT
		(SELECT COUNT(0) FROM Posts WHERE ParentPost_Id = NULL AND Forum_Id = @ForumId AND [Status] != 1) + (SELECT COUNT(0) FROM Posts AS replies INNER JOIN Posts AS topics ON topics.Id = replies.ParentPost_Id WHERE topics.Forum_Id = @ForumId AND topics.[Status] != 1) AS PostCount,
		(SELECT TOP 1 Dates.Created FROM (SELECT Created FROM Posts WHERE ParentPost_Id IS NULL AND Forum_Id = @ForumId AND [Status] != 1 UNION ALL SELECT replies.Created FROM Posts AS replies INNER JOIN Posts AS topics ON topics.Id = replies.ParentPost_Id WHERE topics.Forum_Id = @ForumId AND topics.[Status] != 1) AS Dates ORDER BY Dates.Created DESC) AS LastUpdated
END
GO");
        }
        
        public override void Down()
        {
            Sql(@"ALTER PROCEDURE GetForumStats
	@ForumId bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    SELECT
		(SELECT COUNT(0) FROM Posts WHERE ParentPost_Id = NULL AND Forum_Id = @ForumId) + (SELECT COUNT(0) FROM Posts AS replies INNER JOIN Posts AS topics ON topics.Id = replies.ParentPost_Id WHERE topics.Forum_Id = @ForumId) AS PostCount,
		(SELECT TOP 1 Dates.Created FROM (SELECT Created FROM Posts WHERE ParentPost_Id IS NULL AND Forum_Id = @ForumId UNION ALL SELECT replies.Created FROM Posts AS replies INNER JOIN Posts AS topics ON topics.Id = replies.ParentPost_Id WHERE topics.Forum_Id = @ForumId) AS Dates ORDER BY Dates.Created DESC) AS LastUpdated
END
GO");
        }
    }
}
