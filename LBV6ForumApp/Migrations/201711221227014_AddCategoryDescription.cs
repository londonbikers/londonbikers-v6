namespace LBV6ForumApp.Migrations
{
    using System.Data.Entity.Migrations;
    
    public partial class AddCategoryDescription : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Categories", "Description", c => c.String());
            Sql("UPDATE dbo.Categories SET Description = 'Over a decades'' worth of photo galleries taken by our photographers from events all around the world, including our world-reknown racing galleries.' WHERE Name = 'Galleries'");
            Sql("update forums set [Description] = 'Racing''s in the blood for us. We''ve covered as many different types of motorcycle racing events as we can, and are official photographers for the MCE British Superbike Championship. MotoGP, WSB, BSB, MXGP, you name it, we''ve covered it. Galleries created by our professional photographers and from community submissions.' where [Name] = 'Racing'");
            Sql("update forums set [Description] = NULL where [Name] = 'Other'");
            Sql("update forums set [Description] = 'Detailed photo galleries for new motorcycles.' where [Description] = 'Detailed photo galleries of the latest motorcycles.'");
            Sql("update forums set [Description] = 'Compilation photo galleries of trackday events that the community attends. Do these make you want to go on a trackday with other members? Post up in the Trackdays forum!' where [Name] = 'Trackdays'");
            Sql("update forums set [Description] = 'Photo galleries from motorcycle shows we attend, where the newest motorcycles are showcased.' where [Name] = 'Shows'");
            Sql("update forums set [Description] = 'Photo galleries from bike meets, mainly in London, sometimes further afield. Remember that we have our own meet every Wednesday after work on Stoney Street in Borough Market. Come down!' where [Name] = 'Bike Meets'");
            Sql("update forums set [Description] = 'Exceptional bikes we come across. If you think yours deserves to be featured here, contact us on Twitter (see link at bottom).' where [Name] = 'Featured Bikes'");
        }
        
        public override void Down()
        {
            DropColumn("dbo.Categories", "Description");
        }
    }
}