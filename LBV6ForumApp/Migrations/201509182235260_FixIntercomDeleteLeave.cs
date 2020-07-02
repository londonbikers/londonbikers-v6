namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class FixIntercomDeleteLeave : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PrivateMessages", "Type", c => c.Int(nullable: false));
            AddColumn("dbo.PrivateMessageUsers", "HasDeleted", c => c.Boolean());
            AlterColumn("dbo.PrivateMessages", "Content", c => c.String());

            // delete any headers that have only a single recipient
            Sql(@"delete from PrivateMessageReadBys where PrivateMessage_Id in (
                    select id from PrivateMessages where PrivateMessageHeader_Id in (
                        SELECT
                        [PrivateMessageHeader_Id]
                        FROM [PrivateMessageUsers]
                        group by[PrivateMessageHeader_Id]
                        having count(0) = 1))

                delete from PrivateMessages where PrivateMessageHeader_Id in (
                    SELECT
                    [PrivateMessageHeader_Id]
                    FROM[PrivateMessageUsers]
                    group by[PrivateMessageHeader_Id]
                    having count(0) = 1)

                delete from PrivateMessageUsers where PrivateMessageHeader_Id in (
                    SELECT
                    [PrivateMessageHeader_Id]
                    FROM[PrivateMessageUsers]
                    group by[PrivateMessageHeader_Id]
                    having count(0) = 1)

                delete from PrivateMessageHeaders where id in (
                    SELECT
                    [PrivateMessageHeader_Id]
                    FROM[PrivateMessageUsers]
                    group by[PrivateMessageHeader_Id]
                    having count(0) = 1)");
        }
        
        public override void Down()
        {
            AlterColumn("dbo.PrivateMessages", "Content", c => c.String(nullable: false));
            DropColumn("dbo.PrivateMessageUsers", "HasDeleted");
            DropColumn("dbo.PrivateMessages", "Type");
        }
    }
}