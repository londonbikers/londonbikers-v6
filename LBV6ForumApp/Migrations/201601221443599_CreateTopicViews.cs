namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreateTopicViews : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.TopicViews",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        TopicId = c.Long(nullable: false),
                        UserId = c.String(),
                        Ip = c.String(),
                        When = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.TopicViews");
        }
    }
}
