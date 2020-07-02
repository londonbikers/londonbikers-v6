namespace LBV6.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateLog4NetTable : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE TABLE [dbo].[Log] ( 
                  [ID] [int] IDENTITY (1, 1) NOT NULL ,
                  [Date] [datetime] NOT NULL ,
                  [Thread] [varchar] (255) NOT NULL ,
                  [Level] [varchar] (20) NOT NULL ,
                  [Logger] [varchar] (255) NOT NULL ,
                  [Message] [varchar] (4000) NOT NULL 
                )");
        }
        
        public override void Down()
        {
            DropTable("dbo.Log");
        }
    }
}