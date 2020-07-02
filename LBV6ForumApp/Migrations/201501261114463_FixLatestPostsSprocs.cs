namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixLatestPostsSprocs : DbMigration
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
		ORDER BY LastUpdate DESC
		OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	-------------------------------------------------------------

	SELECT
		COUNT(t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%');

END
GO");

            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithoutRoles]
	@Roles VARCHAR(MAX),
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

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
		ORDER BY LastUpdate DESC
		OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	-------------------------------------------------------------

	SELECT
		COUNT(t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		far.[Role] IS NULL;

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
		ORDER BY LastUpdate
		OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	-------------------------------------------------------------

	SELECT
		COUNT(t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		(far.[Role] IS NULL OR @Roles LIKE '%' + far.[Role] + '%');

END
GO");

            Sql(@"ALTER PROCEDURE [dbo].[GetLatestTopicsForUsersWithoutRoles]
	@Roles VARCHAR(MAX),
	@Offset INT,
	@PageSize INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

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
		ORDER BY LastUpdate
		OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

	-------------------------------------------------------------

	SELECT
		COUNT(t.Id)
		FROM Posts t
		LEFT OUTER JOIN ForumAccessRoles far ON far.Forum_Id = t.Forum_Id
		WHERE
		t.Forum_Id IS NOT NULL AND 
		t.[Status] <> 1 AND
		far.[Role] IS NULL;

END
GO");
        }
    }
}