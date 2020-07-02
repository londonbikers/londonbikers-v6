namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixIncrementTopicViewsSproc : DbMigration
    {
        public override void Up()
        {
            Sql(@"ALTER PROCEDURE IncrementPostViews
	@PostId BIGINT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    UPDATE Posts SET [Views] = [Views] + 1 WHERE Id = @PostId
END
GO");
        }
        
        public override void Down()
        {
            Sql(@"ALTER PROCEDURE IncrementPostViews
	@PostId INT
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    UPDATE Posts SET [Views] = [Views] + 1 WHERE Id = @PostId
END
GO");
        }
    }
}
