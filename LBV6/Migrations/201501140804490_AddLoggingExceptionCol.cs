namespace LBV6.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddLoggingExceptionCol : DbMigration
    {
        public override void Up()
        {
            Sql(@"BEGIN TRANSACTION
                SET QUOTED_IDENTIFIER ON
                SET ARITHABORT ON
                SET NUMERIC_ROUNDABORT OFF
                SET CONCAT_NULL_YIELDS_NULL ON
                SET ANSI_NULLS ON
                SET ANSI_PADDING ON
                SET ANSI_WARNINGS ON
                COMMIT
                BEGIN TRANSACTION
                GO
                ALTER TABLE dbo.[Log] ADD
	                Exception text NULL
                GO
                ALTER TABLE dbo.[Log] SET (LOCK_ESCALATION = TABLE)
                GO
                COMMIT
                ");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Log", "Exception");
        }
    }
}