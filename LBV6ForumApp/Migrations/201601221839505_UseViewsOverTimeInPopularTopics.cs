namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class UseViewsOverTimeInPopularTopics : DbMigration
    {
        public override void Up()
        {
            Sql(@"ALTER PROCEDURE [dbo].[GetPopularTopicsForUsersWithRoles]

    @From DATETIME,
    @Roles VARCHAR(MAX),
    @Offset INT,
    @PageSize INT
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON;

            WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod, ViewsDuringPeriod)
        
            AS
            (
                SELECT
        
                    t.Id,
                    t.LastReplyCreated,
                    (SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS[RepliesDuringPeriod],
                    (SELECT COUNT(0) FROM TopicViews tv WHERE tv.TopicId = t.Id AND[When] >= @From) AS[ViewsDuringPeriod]

            FROM Posts t
            INNER JOIN Posts r ON r.ParentPost_Id = t.Id

            LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id

            WHERE

            t.Forum_Id IS NOT NULL AND

            t.[Status] <> 1 AND

            t.LastReplyCreated IS NOT NULL AND

            r.Created >= @From AND
            (far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')
	)
	SELECT
        Id

        FROM PopularPostsCte

        GROUP BY Id, RepliesDuringPeriod, ViewsDuringPeriod, LastReplyCreated
        ORDER BY RepliesDuringPeriod+ViewsDuringPeriod DESC, LastReplyCreated DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            -------------------------------------------------------------

            WITH PopularPostsCountCte(Id)

    AS
    (
        SELECT

            t.Id

            FROM Posts t

            INNER JOIN Posts r ON r.ParentPost_Id = t.Id

            LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id

            WHERE

            t.Forum_Id IS NOT NULL AND

            t.[Status] <> 1 AND

            t.LastReplyCreated IS NOT NULL AND

            r.Created >= @From AND
            (far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')
    )

    SELECT
        COUNT(DISTINCT Id)
		FROM PopularPostsCountCte
END");

            Sql(@"ALTER PROCEDURE [dbo].[GetPopularTopicsForUsersWithoutRoles]
	@From DATETIME,
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    
	WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod, ViewsDuringPeriod, [Role])
	AS
	(
		SELECT
			t.Id,
			t.LastReplyCreated,
			(SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod],
			(SELECT COUNT(0) FROM TopicViews tv WHERE tv.TopicId = t.Id AND [When] >= @From) AS [ViewsDuringPeriod],
			far.[Role]
			FROM Posts t
			INNER JOIN Posts r ON r.ParentPost_Id = t.Id
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			t.LastReplyCreated IS NOT NULL AND
			r.Created >= @From AND
			(far.[Role] IS NULL)
	)
	SELECT 
		Id 
		FROM PopularPostsCte	
		GROUP BY Id, RepliesDuringPeriod, ViewsDuringPeriod, LastReplyCreated
		ORDER BY RepliesDuringPeriod+ViewsDuringPeriod DESC, LastReplyCreated DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------

	WITH PopularPostsCountCte (Id)
	AS
	(
		SELECT
			t.Id
			FROM Posts t
			INNER JOIN Posts r ON r.ParentPost_Id = t.Id
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			t.LastReplyCreated IS NOT NULL AND
			r.Created >= @From AND
			(far.[Role] IS NULL)
	)
	SELECT 
		COUNT(DISTINCT Id)
		FROM PopularPostsCountCte
END");
        }
        
        public override void Down()
        {
        }
    }
}
