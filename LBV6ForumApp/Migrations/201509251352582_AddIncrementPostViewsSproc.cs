namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddIncrementPostViewsSproc : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE PROCEDURE IncrementPostViews
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
        
        public override void Down()
        {
            Sql("DROP PROCEDURE IncrementPostViews");
        }
    }
}
