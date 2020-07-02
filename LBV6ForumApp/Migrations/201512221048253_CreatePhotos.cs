namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class CreatePhotos : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Photos",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        PostId = c.Long(nullable: true),
                        Created = c.DateTime(nullable: false),
                        Caption = c.String(nullable: true),
                        Position = c.Int(nullable: true),
                        UserId = c.String(nullable: false),
                        Length = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Photos");
        }
    }
}
