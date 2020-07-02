namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddTopicSeenBys : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TopicSeenBys",
                c => new
                    {
                        PostId = c.Long(nullable: false),
                        UserId = c.String(nullable: false, maxLength: 128),
                        LastPostIdSeen = c.Long(nullable: false),
                    })
                .PrimaryKey(t => new { t.PostId, t.UserId });
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TopicSeenBys");
        }
    }
}
