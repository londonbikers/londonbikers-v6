namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixLatestPostsCountSproc : DbMigration
    {
        public override void Up()
        {
            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithRoles]
	@Roles VARCHAR(MAX),
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	WITH LatestPostsCte (Id, LastUpdate)
	AS
	(
		SELECT
			t.Id,
			CASE 
				WHEN t.LastReplyCreated IS NOT NULL THEN t.LastReplyCreated
				ELSE t.Created
			END AS [LastUpdate]
			FROM Posts t
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')
	)
	SELECT 
		Id 
		FROM LatestPostsCte	
		GROUP BY Id, LastUpdate
		ORDER BY LastUpdate DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------
	
	SELECT 
		COUNT(DISTINCT t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE 
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')

END
GO");

            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithoutRoles]
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    
	WITH LatestPostsCte (Id, LastUpdate)
	AS
	(
		SELECT
			t.Id,
			CASE 
				WHEN t.LastReplyCreated IS NOT NULL THEN t.LastReplyCreated
				ELSE t.Created
			END AS [LastUpdate]
			FROM Posts t
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			far.[Role] IS NULL
	)
	SELECT 
		Id 
		FROM LatestPostsCte	
		GROUP BY Id, LastUpdate
		ORDER BY LastUpdate DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------
	
	SELECT 
		COUNT(DISTINCT t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE 
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		far.[Role] IS NULL
END
GO");
        }
        
        public override void Down()
        {
            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithRoles]
	@Roles VARCHAR(MAX),
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

	WITH LatestPostsCte (Id, LastUpdate)
	AS
	(
		SELECT
			t.Id,
			CASE 
				WHEN t.LastReplyCreated IS NOT NULL THEN t.LastReplyCreated
				ELSE t.Created
			END AS [LastUpdate]
			FROM Posts t
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')
	)
	SELECT 
		Id 
		FROM LatestPostsCte	
		GROUP BY Id, LastUpdate
		ORDER BY LastUpdate DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------
	
	SELECT 
		DISTINCT t.Id
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE 
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%')

END
GO");

            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithoutRoles]
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
    
	WITH LatestPostsCte (Id, LastUpdate)
	AS
	(
		SELECT
			t.Id,
			CASE 
				WHEN t.LastReplyCreated IS NOT NULL THEN t.LastReplyCreated
				ELSE t.Created
			END AS [LastUpdate]
			FROM Posts t
			LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
			WHERE 
			t.Forum_Id IS NOT NULL AND 
			t.[Status] <> 1 AND
			far.[Role] IS NULL
	)
	SELECT 
		Id 
		FROM LatestPostsCte	
		GROUP BY Id, LastUpdate
		ORDER BY LastUpdate DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	--------------------------------------------------------------
	
	SELECT 
		DISTINCT t.Id
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE 
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		far.[Role] IS NULL

END
GO");
        }
    }
}
