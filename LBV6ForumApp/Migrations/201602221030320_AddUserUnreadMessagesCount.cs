namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddUserUnreadMessagesCount : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.AspNetUsers", "UnreadMessagesCount", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.AspNetUsers", "UnreadMessagesCount");
        }
    }
}