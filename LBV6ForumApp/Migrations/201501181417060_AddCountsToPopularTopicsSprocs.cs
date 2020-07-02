namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddCountsToPopularTopicsSprocs : DbMigration
    {
        public override void Up()
        {
            Sql(@"SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetPopularTopicsForUsersWithRoles]
	@From DATETIME,
	@Roles VARCHAR(MAX),
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    
	WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod)
	AS
	(
		SELECT
			t.Id,
			t.LastReplyCreated,
			(SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod]
			FROM Posts t
			INNER JOIN Posts r ON r.ParentPost_Id = t.Id
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			t.LastReplyCreated IS NOT NULL AND
			r.Created >= @From AND
			(far.[Role] IS NULL OR @Roles LIKE '%'+far.[Role]+'%')
	)
	SELECT 
		Id 
		FROM PopularPostsCte	
		GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
		ORDER BY RepliesDuringPeriod DESC, LastReplyCreated DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	-------------------------------------------------------------

	WITH PopularPostsCountCte (Id, LastReplyCreated, RepliesDuringPeriod)
	AS
	(
		SELECT
			t.Id,
			t.LastReplyCreated,
			(SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod]
			FROM Posts t
			INNER JOIN Posts r ON r.ParentPost_Id = t.Id
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			t.LastReplyCreated IS NOT NULL AND
			r.Created >= @From AND
			(far.[Role] IS NULL OR @Roles LIKE '%'+far.[Role]+'%')
	)
	SELECT 
		COUNT(0)
		FROM PopularPostsCountCte
		GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
END
GO");

            Sql(@"SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[GetPopularTopicsForUsersWithoutRoles]
	@From DATETIME,
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    
	WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod, [Role])
	AS
	(
		SELECT
			t.Id,
			t.LastReplyCreated,
			(SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod],
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
		GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
		ORDER BY RepliesDuringPeriod DESC, LastReplyCreated DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------

	WITH PopularPostsCountCte (Id, LastReplyCreated, RepliesDuringPeriod)
	AS
	(
		SELECT
			t.Id,
			t.LastReplyCreated,
			(SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod]
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
		COUNT(0)
		FROM PopularPostsCountCte
		GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
END
GO");
        }
        
        public override void Down()
        {
            Sql(@"SET ANSI_NULLS ON
                GO
                SET QUOTED_IDENTIFIER ON
                GO
                ALTER PROCEDURE GetPopularTopicsForUsersWithRoles
	                @From DATETIME,
	                @Roles VARCHAR(MAX),
		            @Offset INT,
		            @PageSize INT
                AS
                BEGIN
	                -- SET NOCOUNT ON added to prevent extra result sets from
	                -- interfering with SELECT statements.
	                SET NOCOUNT ON;
    
	                WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod, [Role])
	                AS
	                (
		                SELECT
			                --TOP 100 PERCENT
			                t.Id,
			                t.LastReplyCreated,
			                (SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod],
			                far.[Role]
			                FROM Posts t
			                INNER JOIN Posts r ON r.ParentPost_Id = t.Id
			                LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			                WHERE 
			                t.Forum_Id IS NOT NULL AND 
			                t.[Status] <> 1 AND
			                t.LastReplyCreated IS NOT NULL AND
			                r.Created >= @From AND
			                (far.[Role] IS NULL OR @Roles LIKE '%'+far.[Role]+'%')
			                --ORDER BY RepliesDuringPeriod DESC, t.LastReplyCreated DESC
	                )
	                SELECT 
		                Id 
		                FROM PopularPostsCte	
		                GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
		                ORDER BY RepliesDuringPeriod DESC, LastReplyCreated DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                END
                GO");

            Sql(@"SET ANSI_NULLS ON
                GO
                SET QUOTED_IDENTIFIER ON
                GO
                ALTER PROCEDURE GetPopularTopicsForUsersWithoutRoles
	                	    @From DATETIME,
		                    @Offset INT,
		                    @PageSize INT
                AS
                BEGIN
	                -- SET NOCOUNT ON added to prevent extra result sets from
	                -- interfering with SELECT statements.
	                SET NOCOUNT ON;
    
	                WITH PopularPostsCte (Id, LastReplyCreated, RepliesDuringPeriod, [Role])
	                AS
	                (
		                SELECT
			                t.Id,
			                t.LastReplyCreated,
			                (SELECT COUNT(0) FROM Posts rc WHERE rc.ParentPost_Id = t.Id AND Created >= @From) AS [RepliesDuringPeriod],
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
		                GROUP BY Id, RepliesDuringPeriod, LastReplyCreated
		                ORDER BY RepliesDuringPeriod DESC, LastReplyCreated DESC
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                END
                GO");
        }
    }
}
