namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddGetUnreadMessagesCountSproc : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE PROCEDURE GetUnreadMessageCountForUser
	@UserId VARCHAR(MAX)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    with HeaderUnreadMessageCounts_CTE (UnreadMessagesInHeader)
	as
	(
		select 	
			(select count(0) 
				from PrivateMessages m 
				left join PrivateMessageReadBys r on r.PrivateMessage_Id =  m.Id and r.UserId = @UserId
				where 
				m.PrivateMessageHeader_Id = h.Id
				and m.Created >= u.Added
				and m.[Type] = 0
				and m.UserId != @UserId
				and r.Id is null) as [UnreadMessages]
			from PrivateMessageHeaders h
			inner join PrivateMessageUsers u on u.PrivateMessageHeader_Id = h.Id
			where u.UserId = @UserId
	)

	SELECT SUM(UnreadMessagesInHeader) FROM HeaderUnreadMessageCounts_CTE
END
GO");
        }
        
        public override void Down()
        {
            Sql("DROP PROCEDURE GetUnreadMessageCountForUser");
        }
    }
}
