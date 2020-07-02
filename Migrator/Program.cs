using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LBV6Library.Models;

namespace Migrator
{
    public class Program
    {
        #region members
        private static string _targetDatabaseName;
        private static StreamWriter _log;
        private static List<Category> _categories;
        #endregion

        static void Main()
        {
            using (_log = new StreamWriter("log-" + DateTime.Now.Ticks + ".txt"))
            {
                var start = DateTime.Now;
                Log("Starting migration...");
                _targetDatabaseName = ConfigurationManager.AppSettings["TargetDatabaseName"];
                //MigratePrivateMessages();
                //MigrateForumStructure();
                //MigratePosts();
                //MigratePrivateMessageAttachments();
                //MigratePostAttachments();
                var runTime = DateTime.Now - start;
                Log("Finished migration");
                Log("Migration ran for: " + runTime);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void MigratePrivateMessages()
        {
            Log("Starting to migrate private messages...");
            var successfulMigrations = 0;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var command = new SqlCommand())
            {
                // just get the non-migrated pm's from the old database
                command.Connection = connection;
                command.CommandText = string.Format(@"SELECT 
	                pm.PrivateMessageID,
	                ia.Id as [v6_author_id],
	                ir.Id as [v6_recipient_id],
	                pm.[Message],
	                pm.DateStamp,
	                pm.ReadFlag
	                FROM [londonbikers_v5_forums].[dbo].[InstantForum_PrivateMessages] pm
                    INNER JOIN [londonbikers_v5_forums].[dbo].[InstantForum_Folders] AS [if] ON [if].FolderID = pm.FolderID
	                INNER JOIN [{0}].[dbo].[AspNetUsers] ia ON ia.LegacyForumId = pm.AuthorID
	                INNER JOIN [{0}].[dbo].[AspNetUsers] ir ON ir.LegacyForumId = pm.RecipientID
	                LEFT OUTER JOIN [{0}].[dbo].[PrivateMessages] v6pm ON v6pm.LegacyMessageId = pm.PrivateMessageID
	                WHERE LegacyMessageId IS NULL AND [if].DeletedItems = 0
	                ORDER BY DateStamp", _targetDatabaseName);

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    // these two allow us to update the header LastMessageSent column when we enumerate on to a new header.
                    long headerId = 0;
                    long lastHeaderId = 0;
                    var lastHeaderLastMessageCreated = DateTime.Now;
                    while (reader.Read())
                    {
                        var authorId = (string)reader["v6_author_id"];
                        var recipientId = (string)reader["v6_recipient_id"];
                        using (var innerConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                        {
                            // do we have a header for this sender/receiver pairing already?
                            innerConnection.Open();

                            #region header
                            headerId = 0;
                            using (var hidCommand = new SqlCommand())
                            {
                                hidCommand.Connection = innerConnection;
                                hidCommand.CommandText = string.Format(@"SELECT PrivateMessageHeader_Id
	                                FROM [{0}].[dbo].[PrivateMessageUsers]
	                                WHERE UserId IN ('{1}', '{2}')
	                                GROUP BY PrivateMessageHeader_Id
	                                HAVING COUNT(DISTINCT UserId) = 2", _targetDatabaseName, authorId, recipientId);
                                var hidResponse = hidCommand.ExecuteScalar();
                                if (hidResponse != null && hidResponse != DBNull.Value)
                                    headerId = Convert.ToInt64(hidResponse);

                                if (lastHeaderId == 0)
                                    lastHeaderId = headerId;
                            }

                            if (headerId == 0)
                            {
                                // use the first message create date as an initial value for the header last message creasted column.
                                var firstMessageCreated = (DateTime)reader["DateStamp"];
                                lastHeaderLastMessageCreated = firstMessageCreated;

                                // create the header and users
                                using (var headerCommand = new SqlCommand("", innerConnection))
                                {
                                    headerCommand.CommandText = string.Format("INSERT INTO [{0}].[dbo].[PrivateMessageHeaders] (LastMessageCreated) OUTPUT INSERTED.ID VALUES (@Created)", _targetDatabaseName);
                                    headerCommand.Parameters.Add(new SqlParameter("@Created", System.Data.SqlDbType.DateTime) { Value = firstMessageCreated });
                                    headerId = Convert.ToInt64(headerCommand.ExecuteScalar());
                                }

                                using (var usersCommand = new SqlCommand())
                                {
                                    usersCommand.Parameters.Clear();
                                    usersCommand.Connection = innerConnection;
                                    usersCommand.CommandText = string.Format("INSERT INTO [{0}].[dbo].[PrivateMessageUsers] (UserId, PrivateMessageHeader_Id, Added) VALUES (@AuthorId, @HeaderId, @Added)", _targetDatabaseName);
                                    usersCommand.Parameters.Add(new SqlParameter("@Added", System.Data.SqlDbType.DateTime) { Value = firstMessageCreated });
                                    usersCommand.Parameters.AddWithValue("@AuthorId", authorId);
                                    usersCommand.Parameters.AddWithValue("@HeaderId", headerId);
                                    usersCommand.ExecuteNonQuery();

                                    usersCommand.Parameters.Clear();
                                    usersCommand.CommandText = string.Format("INSERT INTO [{0}].[dbo].[PrivateMessageUsers] (UserId, PrivateMessageHeader_Id, Added) VALUES (@RecipientId, @HeaderId, @Added)", _targetDatabaseName);
                                    usersCommand.Parameters.Add(new SqlParameter("@Added", System.Data.SqlDbType.DateTime) { Value = firstMessageCreated });
                                    usersCommand.Parameters.AddWithValue("@RecipientId", recipientId);
                                    usersCommand.Parameters.AddWithValue("@HeaderId", headerId);
                                    usersCommand.ExecuteNonQuery();
                                }
                            }                            
                            #endregion

                            #region message
                            // sanitise the message before migration, strip out all but light formatting and images
                            var postContent = RemoveUnwantedMessageFormatting((string)reader["Message"]);

                            // create message
                            int messageId;
                            using (var messageCommand = new SqlCommand())
                            {
                                messageCommand.Connection = innerConnection;
                                messageCommand.CommandText = string.Format("INSERT INTO [{0}].[dbo].[PrivateMessages] (UserId, Created, Content, PrivateMessageHeader_Id, LegacyMessageId) OUTPUT INSERTED.ID VALUES (@UserId, @Created, @Content, @HeaderId, @LegacyId)", _targetDatabaseName);
                                messageCommand.Parameters.AddWithValue("@LegacyId", reader["PrivateMessageID"]);
                                messageCommand.Parameters.AddWithValue("@UserId", authorId);
                                messageCommand.Parameters.AddWithValue("@Created", reader["DateStamp"]);
                                messageCommand.Parameters.AddWithValue("@Content", postContent);
                                messageCommand.Parameters.AddWithValue("@HeaderId", headerId);
                                messageId = Convert.ToInt32(messageCommand.ExecuteScalar());
                            }
                            #endregion

                            #region read-by
                            using (var readByCommand = new SqlCommand(string.Format("INSERT INTO [{0}].[dbo].[PrivateMessageReadBys] (UserId, [When], PrivateMessage_Id) VALUES (@RecipientId, @When, @MessageId)", _targetDatabaseName), innerConnection))
                            {
                                readByCommand.Parameters.AddWithValue("@RecipientId", recipientId);
                                readByCommand.Parameters.AddWithValue("@When", reader["DateStamp"]);
                                readByCommand.Parameters.AddWithValue("@MessageId", messageId);
                                readByCommand.ExecuteNonQuery();
                            }
                            #endregion

                            #region update header last message created
                            if (lastHeaderId > 0 && headerId != lastHeaderId)
                            {
                                // new header, update LastMessageCreated for last header
                                UpdateHeaderLastMessageCreated(lastHeaderId, lastHeaderLastMessageCreated, innerConnection);

                                // move on to new header
                                lastHeaderId = headerId;
                            }
                            else
                            {
                                // same header, keep track of the latest header message date
                                lastHeaderLastMessageCreated = (DateTime)reader["DateStamp"];
                            }
                            #endregion

                            var shortMessage = postContent.Replace("\r\n", string.Empty);
                            if (shortMessage.Length > 50)
                                shortMessage = shortMessage.Substring(0, 49);
                            successfulMigrations++;
                            Log(string.Format("Successfully migrated message {0}: {1}", successfulMigrations,  shortMessage));
                        }
                    }

                    using (var updateHeaderConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                    {
                        updateHeaderConnection.Open();
                        UpdateHeaderLastMessageCreated(lastHeaderId, lastHeaderLastMessageCreated, updateHeaderConnection);
                    }
                }
            }

            Log(string.Format("Successfully migrated {0} private messages.", successfulMigrations));
        }

        private static void MigrateForumStructure()
        {
            _categories = new List<Category>();
            using (var readConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                readConnection.Open();

                // do we have any structure?
                bool haveStructure;
                using (var haveStructureCmd = new SqlCommand("SELECT COUNT(0) FROM [Forums]", readConnection))
                    haveStructure = Convert.ToInt32(haveStructureCmd.ExecuteScalar()) > 0;

                #region new structure
                if (!haveStructure)
                {
                    var c1 = new Category { Name = "Motorcycling", Order = 0, Forums = new List<Forum>() };
                    c1.Forums.Add(new Forum { Name = "Motorcycles", Order = 0 });
                    c1.Forums.Add(new Forum { Name = "Stolen!", Order = 1 });
                    c1.Forums.Add(new Forum { Name = "Modification Showroom", Order = 2 });
                    c1.Forums.Add(new Forum { Name = "Newbies", Order = 3 });
                    _categories.Add(c1);

                    var c2 = new Category { Name = "Ride-outs & Events", Order = 1, Forums = new List<Forum>() };
                    c2.Forums.Add(new Forum { Name = "Ride-outs", Order = 0 });
                    c2.Forums.Add(new Forum { Name = "Events", Order = 1 });
                    c2.Forums.Add(new Forum { Name = "Ride Reports", Order = 2 });
                    _categories.Add(c2);

                    var c3 = new Category { Name = "Help & Advice", Order = 2, Forums = new List<Forum>() };
                    c3.Forums.Add(new Forum { Name = "Questions & Answers", Order = 0 });
                    c3.Forums.Add(new Forum { Name = "Road Watch", Order = 1 });
                    c3.Forums.Add(new Forum { Name = "Legal", Order = 2 });
                    c3.Forums.Add(new Forum { Name = "Praise or Shame", Order = 3 });
                    _categories.Add(c3);

                    var c4 = new Category { Name = "Racing", Order = 3, Forums = new List<Forum>() };
                    c4.Forums.Add(new Forum { Name = "MotoGP, WSB & BSB", Order = 0 });
                    c4.Forums.Add(new Forum { Name = "Club Racing", Order = 1 });
                    _categories.Add(c4);

                    var c5 = new Category { Name = "Classified Ads", Order = 4, Forums = new List<Forum>() };
                    c5.Forums.Add(new Forum { Name = "Motorcycles", Order = 0 });
                    c5.Forums.Add(new Forum { Name = "Parts & Accessories", Order = 1 });
                    c5.Forums.Add(new Forum { Name = "Other", Order = 2 });
                    _categories.Add(c5);

                    var c6 = new Category { Name = "Off-topic", Order = 5, Forums = new List<Forum>() };
                    c6.Forums.Add(new Forum { Name = "Anything Else", Order = 0 });
                    c6.Forums.Add(new Forum { Name = "Charity", Order = 1 });
                    c6.Forums.Add(new Forum { Name = "Announcements", Order = 2 });
                    _categories.Add(c6);

                    var c7 = new Category { Name = "Staff", Order = 6, Forums = new List<Forum>() };
                    var f1 = new Forum { Name = "All-staff", Order = 0, AccessRoles = new List<ForumAccessRole>() };
                    f1.AccessRoles.Add(new ForumAccessRole { Role = "Contributor"});
                    f1.AccessRoles.Add(new ForumAccessRole { Role = "Moderator"});
                    f1.AccessRoles.Add(new ForumAccessRole { Role = "Administrator"});
                    c7.Forums.Add(f1);

                    var f2 = new Forum { Name = "Forum Administration", Order = 1, AccessRoles = new List<ForumAccessRole>() };
                    f2.AccessRoles.Add(new ForumAccessRole { Role = "Moderator" });
                    f2.AccessRoles.Add(new ForumAccessRole { Role = "Administrator" });
                    c7.Forums.Add(f2);

                    var f3 = new Forum { Name = "Archived Posts", Order = 2, AccessRoles = new List<ForumAccessRole>() };
                    f3.AccessRoles.Add(new ForumAccessRole { Role = "Moderator" });
                    f3.AccessRoles.Add(new ForumAccessRole { Role = "Administrator" });
                    c7.Forums.Add(f3);

                    var f4 = new Forum { Name = "Editorial", Order = 3, AccessRoles = new List<ForumAccessRole>() };
                    f4.AccessRoles.Add(new ForumAccessRole { Role = "Moderator" });
                    f4.AccessRoles.Add(new ForumAccessRole { Role = "Administrator" });
                    c7.Forums.Add(f4);
                    _categories.Add(c7);

                    using (var structureCmd = new SqlCommand("", readConnection))
                    {
                        foreach (var c in _categories)
                        {
                            structureCmd.Parameters.Clear();
                            structureCmd.CommandText = "INSERT INTO [Categories] ([Name], [Order], [Created]) OUTPUT INSERTED.ID VALUES (@Name, @Order, @Created)";
                            structureCmd.Parameters.AddWithValue("@Name", c.Name);
                            structureCmd.Parameters.AddWithValue("@Order", c.Order);
                            structureCmd.Parameters.AddWithValue("@Created", DateTime.Now);
                            c.Id = Convert.ToInt64(structureCmd.ExecuteScalar());
                            Log(string.Format("Created {0} category.", c.Name));

                            foreach (var f in c.Forums)
                            {
                                structureCmd.Parameters.Clear();
                                structureCmd.CommandText = "INSERT INTO [Forums] ([Name], [Order], [Created], [Category_Id]) OUTPUT INSERTED.ID VALUES (@Name, @Order, @Created, @CategoryId)";
                                structureCmd.Parameters.AddWithValue("@Name", f.Name);
                                structureCmd.Parameters.AddWithValue("@Order", f.Order);
                                structureCmd.Parameters.AddWithValue("@Created", DateTime.Now);
                                structureCmd.Parameters.AddWithValue("@CategoryId", c.Id);

                                f.Id = Convert.ToInt64(structureCmd.ExecuteScalar());
                                Log(string.Format("Created {0} forum.", f.Name));

                                if (f.AccessRoles == null) continue;
                                foreach (var ar in f.AccessRoles)
                                {
                                    structureCmd.Parameters.Clear();
                                    structureCmd.CommandText = "INSERT INTO [ForumAccessRoles] ([Forum_Id], [Role]) VALUES (@ForumId, @Role)";
                                    structureCmd.Parameters.AddWithValue("@ForumId", f.Id);
                                    structureCmd.Parameters.AddWithValue("@Role", ar.Role);
                                    structureCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
                #endregion

                //var d1 = "debugstop";

                #region existing structure
                if (!haveStructure) return;
                using (var getCategoriesCmd = new SqlCommand("SELECT [Id], [Name] FROM [Categories]", readConnection))
                using (var getCategoriesReader = getCategoriesCmd.ExecuteReader())
                while (getCategoriesReader.Read())
                {
                    var c = new Category
                    {
                        Name = (string)getCategoriesReader["Name"],
                        Id = (long)getCategoriesReader["Id"]
                    };
                    _categories.Add(c);
                }

                const string getForumSqlTemplate = "SELECT [Id], [Name] FROM [Forums] WHERE Category_Id = {0}";
                using (var getForumsCmd = new SqlCommand(getForumSqlTemplate, readConnection))
                foreach (var c in _categories)
                {
                    c.Forums = new List<Forum>();
                    getForumsCmd.CommandText = string.Format(getForumSqlTemplate, c.Id);
                    using (var getForumsReader = getForumsCmd.ExecuteReader())
                        while (getForumsReader.Read())
                            c.Forums.Add(new Forum { Id = (long)getForumsReader["Id"], Name = (string)getForumsReader["Name"] });
                }
                #endregion
            }

            //var d2 = "debugstop";
        }

        private static void MigratePosts()
        {
            MigrateForum("Bike Talk", "Motorcycling", "Motorcycles");
            MigrateForum("Pictures and Videos", "Motorcycling", "Motorcycles");
            MigrateForum("Adventure & Offroad", "Motorcycling", "Motorcycles");
            MigrateForum("Cruisers & Tourers", "Motorcycling", "Motorcycles");
            MigrateForum("Scooters & Small Bikes", "Motorcycling", "Motorcycles");
            MigrateForum("Sports Bikes", "Motorcycling", "Motorcycles");
            MigrateForum("Fighters Corner", "Motorcycling", "Motorcycles");
            MigrateForum("SuperMoto", "Motorcycling", "Motorcycles");
            MigrateForum("Courier's Corner", "Motorcycling", "Motorcycles");
            MigrateForum("Classic Bikes", "Motorcycling", "Motorcycles");
            MigrateForum("Products & Upgrades", "Motorcycling", "Motorcycles");
            MigrateForum("Stolen Bikes!", "Motorcycling", "Stolen!");
            MigrateForum("Modification Showroom", "Motorcycling", "Modification Showroom");
            MigrateForum("Newbies", "Motorcycling", "Newbies");

            MigrateForum("Ride Outs, Meets & Events", "Ride-outs & Events", "Ride-outs");
            MigrateForum("LB Events", "Ride-outs & Events", "Events");
            MigrateForum("Trackdays", "Ride-outs & Events", "Events");
            MigrateForum("Ride Reports, Home & Abroad", "Ride-outs & Events", "Ride Reports");

            MigrateForum("Questions & Answers", "Help & Advice", "Questions & Answers");
            MigrateForum("Road Watch", "Help & Advice", "Road Watch");
            MigrateForum("Bikers and the Law", "Help & Advice", "Legal");
            MigrateForum("Praise or Shame", "Help & Advice", "Praise or Shame");

            MigrateForum("Professional Racing", "Racing", "MotoGP, WSB & BSB");
            MigrateForum("Club Racing", "Racing", "Club Racing");

            MigrateForum("Bikes", "Classified Ads", "Motorcycles");
            MigrateForum("Parts & Accessories", "Classified Ads", "Parts & Accessories");
            MigrateForum("Anything Else", "Classified Ads", "Other");
            MigrateForum("Jobs", "Classified Ads", "Other");

            MigrateForum("General Chat", "Off-topic", "Anything Else");
            MigrateForum("Off Topic", "Off-topic", "Anything Else");
            MigrateForum("Going Out", "Off-topic", "Anything Else");
            MigrateForum("Photography & Video", "Off-topic", "Anything Else");
            MigrateForum("Console & Computer Games", "Off-topic", "Anything Else");
            MigrateForum("Gadgets & Tech", "Off-topic", "Anything Else");
            MigrateForum("Music", "Off-topic", "Anything Else");
            MigrateForum("LB Charity Efforts", "Off-topic", "Charity");
            MigrateForum("Site News", "Off-topic", "Announcements");

            MigrateForum("All-Staff Forum", "Staff", "All-staff");
            MigrateForum("LB needs help with site", "Staff", "All-staff");
            MigrateForum("Forum Administration", "Staff", "Forum Administration");
            MigrateForum("Archived Posts", "Staff", "Archived Posts");
            MigrateForum("Editorial & Photographic", "Staff", "Editorial");

            Log("Migrated all forums.");
        }

        private static void MigratePrivateMessageAttachments()
        {
            var successfulMigrations = 0;
            var failedMigrations = 0;
            var readSql = string.Format(@"select 
	            v6pm.Id as [v6_id],
	            a.AttachmentID as [legacy_id],
	            a.[Filename],
	            a.ContentType,
	            a.[Views],
                a.DateStamp
	            from londonbikers_v5_forums.dbo.InstantForum_AttachmentsPosts as ap
	            inner join londonbikers_v5_forums.dbo.InstantForum_Attachments as a on a.AttachmentID = ap.AttachmentID
	            inner join {0}.dbo.PrivateMessages as v6pm on v6pm.LegacyMessageId = ap.PostID
	            left outer join {0}.dbo.PrivateMessageAttachments as v6pma on v6pma.LegacyId = a.AttachmentID
	            where ap.IsPrivateMessage = 1 and 
	            v6pma.LegacyId is null and
	            (
		            a.[Filename] like '%.png' or
		            a.[Filename] like '%.gif' or
		            a.[Filename] like '%.jpg' or
		            a.[Filename] like '%.jpeg'
	            )		
	            order by a.DateStamp", _targetDatabaseName);

            var writeSql = string.Format(@"insert into {0}.dbo.PrivateMessageAttachments 
	            (Created, ContentType, LegacyId, PrivateMessage_Id, [Filename]) values 
	            (@Created, @ContentType, @LegacyId, @PrivateMessageId, @Filename)", _targetDatabaseName);

            using (var readConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var writeConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                readConnection.Open();
                writeConnection.Open();

                using (var readCommand = new SqlCommand(readSql, readConnection))
                using (var reader = readCommand.ExecuteReader())
                using (var writeCommand = new SqlCommand(writeSql, writeConnection))
                {
                    while (reader.Read())
                    {
                        // copy the attachment file to it's new location
                        var date = (DateTime)reader["DateStamp"];
                        var root = ConfigurationManager.AppSettings["AttachmentStoreRoot"];
                        var oldPath = string.Format(@"{0}\{1}", ConfigurationManager.AppSettings["LegacyAttachmentsFolder"], reader["Filename"]);
                        var newPath = string.Format(@"{0}\{1}\{2}\{3}", root, date.Year, date.Month, date.Day);
                        if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
                        newPath += "\\" + reader["Filename"];

                        writeCommand.Parameters.Clear();
                        writeCommand.Parameters.AddWithValue("@Created", reader["DateStamp"]);
                        writeCommand.Parameters.AddWithValue("@ContentType", reader["ContentType"]);
                        writeCommand.Parameters.AddWithValue("@LegacyId", reader["legacy_id"]);
                        writeCommand.Parameters.AddWithValue("@PrivateMessageId", reader["v6_id"]);
                        writeCommand.Parameters.AddWithValue("@Filename", reader["Filename"]);
                        writeCommand.ExecuteNonQuery();
                        Log("Migrated PM attachment record: " + reader["Filename"]);

                        try
                        {
                            File.Copy(oldPath, newPath);
                            successfulMigrations++;
                        }
                        catch (FileNotFoundException)
                        {
                            failedMigrations++;
                        }
                        catch (IOException ex)
                        {
                            if (ex.Message.EndsWith("already exists."))
                                failedMigrations++;
                            else
                                throw;
                        }
                    }
                }
            }

            Log(string.Format("Migrated {0} private message attachments. {1} couldn't be migrated.", successfulMigrations, failedMigrations));
        }

        private static void MigratePostAttachments()
        {
            var successfulMigrations = 0;
            var failedMigrations = 0;
            var readSql = string.Format(@"select 
	            v6p.Id as [v6_id],
	            a.AttachmentID as [legacy_id],
	            a.[Filename],
	            a.ContentType,
	            a.[Views],a.DateStamp
	            from londonbikers_v5_forums.dbo.InstantForum_AttachmentsPosts as ap
	            inner join londonbikers_v5_forums.dbo.InstantForum_Attachments as a on a.AttachmentID = ap.AttachmentID
	            inner join {0}.dbo.Posts as v6p on v6p.LegacyPostId = ap.PostID
	            left outer join {0}.dbo.PostAttachments as v6pa on v6pa.LegacyId = a.AttachmentID
	            where ap.IsPrivateMessage = 0 and 
	            v6pa.LegacyId is null and 
	            ( 
		            a.[Filename] like '%.png' or
		            a.[Filename] like '%.gif' or
		            a.[Filename] like '%.jpg' or
		            a.[Filename] like '%.jpeg'
	            )
	            order by a.DateStamp", _targetDatabaseName);

            var writeSql = string.Format(@"insert into {0}.dbo.PostAttachments 
	            (Created, ContentType, LegacyId, Post_Id, [Filename], [Views]) values 
	            (@Created, @ContentType, @LegacyId, @PostId, @Filename, @Views)", _targetDatabaseName);

            using (var readConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var writeConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                readConnection.Open();
                writeConnection.Open();

                using (var readCommand = new SqlCommand(readSql, readConnection))
                using (var reader = readCommand.ExecuteReader())
                using (var writeCommand = new SqlCommand(writeSql, writeConnection))
                    while (reader.Read())
                    {
                        // copy the attachment file to it's new location
                        var date = (DateTime)reader["DateStamp"];
                        var root = ConfigurationManager.AppSettings["AttachmentStoreRoot"];
                        var oldPath = string.Format(@"{0}\{1}", ConfigurationManager.AppSettings["LegacyAttachmentsFolder"], reader["Filename"]);
                        var newPath = string.Format(@"{0}\{1}\{2}\{3}", root, date.Year, date.Month, date.Day);
                        if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);
                        newPath += "\\" + reader["Filename"];

                        try
                        {
                            File.Copy(oldPath, newPath);

                            writeCommand.Parameters.Clear();
                            writeCommand.Parameters.AddWithValue("@Created", reader["DateStamp"]);
                            writeCommand.Parameters.AddWithValue("@ContentType", reader["ContentType"]);
                            writeCommand.Parameters.AddWithValue("@LegacyId", reader["legacy_id"]);
                            writeCommand.Parameters.AddWithValue("@PostId", reader["v6_id"]);
                            writeCommand.Parameters.AddWithValue("@Filename", reader["Filename"]);
                            writeCommand.Parameters.AddWithValue("@Views", reader["Views"]);
                            writeCommand.ExecuteNonQuery();

                            Log("Migrated post attachment: " + reader["Filename"]);
                            successfulMigrations++;
                        }
                        catch (FileNotFoundException)
                        {
                            failedMigrations++;
                        }
                        catch (IOException ex)
                        {
                            if (ex.Message.EndsWith("already exists."))
                                failedMigrations++;
                            else
                                throw;
                        }
                    }
            }

            Log(string.Format("Migrated {0} post attachments. {1} couldn't be migrated.", successfulMigrations, failedMigrations));
        }

        #region utilities
        private static void MigrateForum(string sourceForumName, string targetCategoryName, string targetForumName)
        {
            #region sql templates
            const string readPostSql = @"SELECT
	            np.Id,
	            om.PostID,
	            ot.TopicID,
	            v6au.Id as [v6_author_id],
	            ot.[Views],
	            ot.[Title],
	            om.[Message],
	            ot.[DateStamp],
	            ot.IsPinned,
	            ot.IsLocked,
	            ot.EditDateStamp
	            FROM [londonbikers_v5_forums].[dbo].[InstantForum_Topics] ot
	            INNER JOIN [londonbikers_v5_forums].[dbo].[InstantForum_Messages] om ON om.PostID = ot.PostID
	            INNER JOIN [londonbikers_v5_forums].[dbo].[InstantForum_Forums] [of] ON [of].ForumID = ot.ForumID
	            INNER JOIN [{0}].[dbo].[AspNetUsers] v6au ON v6au.LegacyForumId = ot.UserID
	            LEFT OUTER JOIN [{0}].[dbo].[AspNetUsers] v6eu ON v6eu.LegacyForumId = ot.EditUserID
	            LEFT OUTER JOIN [{0}].[dbo].[Posts] np ON np.LegacyPostId = ot.PostID
	            WHERE [of].Name ='{1}' AND np.Id IS NULL
	            ORDER BY ot.PostID";

            const string writeTopicSql = @"INSERT INTO [Posts]	
                    (
	                    [Created],
	                    [Subject],
	                    [Content],
	                    [IsSticky],
	                    [Forum_Id],
	                    [User_Id],
	                    [Views],
	                    [LegacyPostId],
                        [EditedOn]
                    )
                    VALUES 
                    (
	                    @Created,
	                    @Subject,
	                    @Content,
	                    @IsSticky,
	                    @ForumId,
	                    @UserId,
	                    @Views,
	                    @LegacyPostId,
	                    @EditedOn
                    )";

            const string writeReplySql = @"INSERT INTO [Posts] 
                    (
	                    [Created],
	                    [Content],
	                    [User_Id],
	                    [LegacyPostId],
	                    [EditedOn],
	                    [ParentPost_Id]
                    )
                    SELECT
	                    v.Created,
	                    v.Content,
	                    v.UserId,
	                    v.LegacyPostId,
	                    v.EditedOn,
	                    t.Id
	                    FROM
	                    (
		                    SELECT
			                    @Created as [Created],
			                    @Content as [Content],
			                    @UserId as [UserId],
			                    @LegacyPostId as [LegacyPostId],
			                    @EditedOn as [EditedOn],
			                    @TopicId as [TopicId]
	                    ) as [v]
	                    INNER JOIN [Posts] t ON t.LegacyPostId = v.TopicId";
            #endregion

            var topicsMigrated = 0;
            var repliesMigrated = 0;
            using (var readConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var writeConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                readConnection.Open();
                writeConnection.Open();
                var targetForumId = _categories.Single(q => q.Name.Equals(targetCategoryName)).Forums.Single(q => q.Name.Equals(targetForumName)).Id;
                using (var readCommand = new SqlCommand(string.Format(readPostSql, _targetDatabaseName, sourceForumName.Replace("'", "''")), readConnection))
                using (var reader = readCommand.ExecuteReader())
                using (var writeCommand = new SqlCommand("", writeConnection))
                    while (reader.Read())
                    {
                        writeCommand.Parameters.Clear();
                        var message = RemoveUnwantedMessageFormatting((string)reader["Message"]);
                        var status = reader["IsLocked"] != DBNull.Value && (bool) reader["IsLocked"] ? PostStatus.Closed : PostStatus.Active;
                        if ((int)reader["PostID"] == (int)reader["TopicID"])
                        {
                            // topic
                            writeCommand.CommandText = writeTopicSql;
                            writeCommand.Parameters.AddWithValue("@Created", reader["DateStamp"]);
                            writeCommand.Parameters.AddWithValue("@Subject", reader["Title"]);
                            writeCommand.Parameters.AddWithValue("@Content", message);
                            writeCommand.Parameters.AddWithValue("@IsSticky", reader["IsPinned"]);
                            writeCommand.Parameters.AddWithValue("@ForumId", targetForumId);
                            writeCommand.Parameters.AddWithValue("@UserId", reader["v6_author_id"]);
                            writeCommand.Parameters.AddWithValue("@Views", reader["Views"]);
                            writeCommand.Parameters.AddWithValue("@LegacyPostId", reader["PostID"]);
                            writeCommand.Parameters.AddWithValue("@EditedOn", reader["EditDateStamp"]);
                            writeCommand.Parameters.AddWithValue("@Status", (byte)status);
                            Log("Migrated topic: " + reader["Title"]);
                            topicsMigrated++;
                        }
                        else
                        {
                            // reply
                            writeCommand.CommandText = writeReplySql;
                            writeCommand.Parameters.AddWithValue("@Created", reader["DateStamp"]);
                            writeCommand.Parameters.AddWithValue("@Content", message);
                            writeCommand.Parameters.AddWithValue("@UserId", reader["v6_author_id"]);
                            writeCommand.Parameters.AddWithValue("@LegacyPostId", reader["PostID"]);
                            writeCommand.Parameters.AddWithValue("@EditedOn", reader["EditDateStamp"]);
                            writeCommand.Parameters.AddWithValue("@TopicId", reader["TopicId"]);
                            Log("Migrated reply to: " + reader["TopicId"]);
                            repliesMigrated++;
                        }

                        writeCommand.ExecuteNonQuery();
                    }
            }

            Log(string.Format("Migrated {0} topics and {1} replies into the '{2}/{3}' forum from the '{4}' forum.", topicsMigrated, repliesMigrated, targetCategoryName, targetForumName, sourceForumName));
        }

        public static string RemoveUnwantedMessageFormatting(string message)
        {
            // sanitise the message before migration...

            // replace containers/line breaks with new lines
            var postContent = Regex.Replace(message, "<br>|<br/>|<br />", "\r\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            postContent = Regex.Replace(postContent, "</p>", "\r\n\r\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            postContent = Regex.Replace(postContent, "<div(.*?)>", "\r\n", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            postContent = postContent.Replace("&nbsp;", " ");
            postContent = postContent.Replace("&amp;", "&");

            // convert old-style object youtube embeds to forum code
            postContent = Regex.Replace(postContent, "<object\\s(?:.*?)value=\"(?:.*?)(www\\.youtube\\.com.*?)\"(?:.*?)</object>", "[YouTube]https://$1[/YouTube]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // remove all but light formatting or acceptable html
            postContent = Regex.Replace(postContent, @"<(?!\/?(b|img|strong|iframe|a)(?=>|\s.*>))\/?.*?>", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // remove alt and title attributes from remaining elements
            const string altStripPattern = "\\w*(alt|title|class|rel|style|bgcolor|align|dir|data|color|border|id|name)=\".*?\"\\w*";
            postContent = Regex.Replace(postContent, altStripPattern, string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            // remove empty lines
            postContent = Regex.Replace(postContent, "^ +\r\n", string.Empty, RegexOptions.Compiled | RegexOptions.Multiline);

            // normalise weird line returns
            postContent = postContent.Replace("\r\n\n", "\r\n");

            // remove excessive line breaks (two is okay, three or more is bad)
            postContent = Regex.Replace(postContent, "(\r\n){3,}", "\r\n\r\n", RegexOptions.Compiled);

            // remove excessive spaces
            postContent = Regex.Replace(postContent, " {2,}", " ", RegexOptions.Compiled);

            // remove leading/trailing white-space
            postContent = postContent.Trim('\r').Trim('\n').Trim();
            return postContent;
        }

        private static void Log(string message)
        {
            Console.WriteLine(message);
            _log.WriteLine(message);
        }

        private static void UpdateHeaderLastMessageCreated(long headerId, DateTime lastMessageCreated, SqlConnection connection)
        {
            using (var command = new SqlCommand())
            {
                command.Connection = connection;
                command.CommandText = string.Format("UPDATE [{0}].[dbo].[PrivateMessageHeaders] SET LastMessageCreated = @LastMessageCreated WHERE Id = @HeaderId", _targetDatabaseName);
                command.Parameters.AddWithValue("@HeaderId", headerId);
                command.Parameters.AddWithValue("@LastMessageCreated", lastMessageCreated);
                command.ExecuteNonQuery();
            }
        }
        #endregion
    }
}