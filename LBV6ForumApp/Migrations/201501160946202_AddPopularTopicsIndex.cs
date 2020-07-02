namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddPopularTopicsIndex : DbMigration
    {
        public override void Up()
        {
            Sql(@"CREATE NONCLUSTERED INDEX [IDX_PopularTopicsCreatedParentPost_Id]
                ON [dbo].[Posts] ([Created])
                INCLUDE ([ParentPost_Id])");
        }
        
        public override void Down()
        {
            DropIndex("dbo.Posts", "IDX_PopularTopicsCreatedParentPost_Id");
        }
    }
}