using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using net.openstack.Core.Exceptions.Response;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GalleriesMigrator
{
    internal class Program
    {
        private static readonly string TargetDatabaseName = ConfigurationManager.AppSettings["TargetDatabaseName"];
        private const string CategoryName = "Galleries";
        private static Category _category;

        private static void Main()
        {
            // - create category if necessary
            // - create forums if necessary
            // - create photos (from published galleries)
            // - create photo comments
            // - create posts (published galleries)

            CreateCategoriesAsync().Wait();
            CreateForumsAsync().Wait();
            MigratePhotosAsync().Wait();
            MigrateGalleriesAsync().Wait();
            MigrateGalleryCommentsAsync().Wait();
            MigratePhotoCommentsAsync().Wait();
            UpdateForumStatsAsync().Wait();

            RemoveOrphanedPhotoRecordsAsync().Wait();
            RemoveEmptyGalleriesAsync().Wait();
            ActivateAllGalleriesAsync().Wait();

            Console.WriteLine(@"Press any key to exit.");
            Console.ReadKey(true);
        }

        private static async Task CreateCategoriesAsync()
        {
            _category = ForumServer.Instance.Categories.Categories.SingleOrDefault(q => q.Name.Equals(CategoryName));
            if (_category != null)
            {
                Console.WriteLine($@"Categories: {CategoryName} category already exists. Skipping.");
                return;
            }

            _category = new Category
            {
                Name = CategoryName,
                Description = "Over a decades worth of photo galleries taken by our photographers from motorcycle events all around the world, including our racing galleries that are reknown the world over.",
                IsGalleryCategory = true
            };
            await ForumServer.Instance.Categories.UpdateCategoryAsync(_category);
            Console.WriteLine($@"Categories: Created {_category.Name} category.");
        }

        private static async Task CreateForumsAsync()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT f_name, f_description FROM [londonbikers_v5].[dbo].[apollo_gallery_categories]";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var name = reader["f_name"].ToString();

                            // do we already have a target forum for this one?
                            if (_category.Forums.Any(q => q.Name.Equals(name)))
                            {
                                Console.WriteLine($@"Forums: {name} forum already exists. Skipping.");
                            }
                            else
                            {
                                var f = new Forum
                                {
                                    Name = name,
                                    Description = (string)reader["f_description"],
                                    CategoryId = _category.Id
                                };

                                if (name.Equals("Racing"))
                                    f.ProtectTopicPhotos = true;

                                // posting in gallery forums must be restricted to people with the photographer role
                                f.PostRoles.Add(new ForumPostRole { ForumId = f.Id, Role = "Photographers" });

                                // create the forum!
                                await ForumServer.Instance.Forums.UpdateForumAsync(f);
                                Console.WriteLine($@"Forums: Created forum: {name}.");
                            }
                        }
                    }
                }

                // now make some forum description changes
                using (var tidyupCommand = new SqlCommand())
                {
                    tidyupCommand.Connection = connection;

                    tidyupCommand.CommandText = "UPDATE dbo.Categories SET Description = 'Over a decades'' worth of photo galleries taken by our photographers from events all around the world, including our world-reknown racing galleries.' WHERE Name = 'Galleries'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Racing''s in the blood for us. We''ve covered as many different types of motorcycle racing events as we can, and are official photographers for the MCE British Superbike Championship. MotoGP, WSB, BSB, MXGP, you name it, we''ve covered it. Galleries created by our professional photographers and from community submissions.' where [Name] = 'Racing'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = NULL where [Name] = 'Other'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Detailed photo galleries for new motorcycles.' where[Description] = 'Detailed photo galleries of the latest motorcycles.'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Compilation photo galleries of trackday events that the community attends. Do these make you want to go on a trackday with other members? Post up in the Trackdays forum!' where [Name] = 'Trackdays'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Photo galleries from motorcycle shows we attend, where the newest motorcycles are showcased.' where [Name] = 'Shows'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Photo galleries from bike meets, mainly in London, sometimes further afield. Remember that we have our own meet every Wednesday after work on Stoney Street in Borough Market. Come down!' where [Name] = 'Bike Meets'";
                    tidyupCommand.ExecuteNonQuery();

                    tidyupCommand.CommandText = "update forums set [Description] = 'Exceptional bikes we come across. If you think yours deserves to be featured here, contact us on Twitter (see link at bottom).' where [Name] = 'Featured Bikes'";
                    tidyupCommand.ExecuteNonQuery();
                }
            }
        }

        private static async Task MigratePhotosAsync()
        {
            var files = ForumServer.Instance.Photos.GetFilesProvider();
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync("londonbikers.com");
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    // query for gallery photos from published galleries that haven't yet been migrated
                    command.Connection = connection;
                    command.CommandText = $@"SELECT 
	                    gi.[ID]
	                    ,gi.[Name]
	                    ,gi.[Credit]
	                    ,gi.[Comment]
	                    ,gi.[CreationDate]
	                    ,gi.[Filename1600]
	                    ,gi.[Views]
	                    FROM [londonbikers_v5].[dbo].[GalleryImages] gi
	                    INNER JOIN [londonbikers_v5].[dbo].[apollo_galleries] g ON g.ID = gi.GalleryID
	                    LEFT OUTER JOIN [{TargetDatabaseName}].[dbo].[Photos] p ON p.LegacyGalleryPhotoId = gi.ID
	                    WHERE g.f_status = 1 AND p.Id IS NULL AND gi.Filename1600 IS NOT NULL AND gi.Filename1600 <> '';";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var legacyPhotoId = (long)reader["ID"];
                            var name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() : null;
                            var credit = reader["Credit"] != DBNull.Value ? reader["Credit"].ToString() : null;
                            var comment = reader["Comment"] != DBNull.Value ? reader["Comment"].ToString() : null;
                            var caption = (name + " " + comment).Trim();
                            var created = (DateTime)reader["CreationDate"];
                            var filename1600 = reader["Filename1600"].ToString();
                            var filePath = Path.Combine(ConfigurationManager.AppSettings["GalleryPhotosRoot"], filename1600);

                            if (!string.IsNullOrEmpty(credit) &&
                                credit.Equals("jay adair", StringComparison.InvariantCultureIgnoreCase))
                                credit = "Jay";

                            if (!File.Exists(filePath))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($@"File '{filename1600}' doesn't exist! Skipping");
                                Console.ResetColor();
                                continue;
                            }

                            #region create database record
                            // create photo record
                            // determine the image properties
                            Size size;
                            using (var image = Image.FromFile(filePath))
                                size = Utilities.GetAutoRotatedImageDimensions(image);

                            long contentLength;
                            using (var file = File.OpenRead(filePath))
                                contentLength = file.Length;

                            var photo = new Photo
                            {
                                ContentType = MimeMapping.GetMimeMapping(filename1600),
                                Length = contentLength,
                                UserId = user.Id,
                                Width = size.Width,
                                Height = size.Height,
                                Created = created,
                                LegacyGalleryPhotoId = legacyPhotoId
                            };

                            if (!string.IsNullOrEmpty(caption))
                                photo.Caption = caption;

                            if (!string.IsNullOrEmpty(credit))
                                photo.Credit = credit;

                            // transaction needed here that encapsulates file upload process
                            // so failed uploads don't result in records with no files
                            using (var db = new ForumContext())
                            {
                                db.Photos.Add(photo);
                                await db.SaveChangesAsync();
                            }

                            #endregion

                            #region upload file to object store
                            try
                            {
                                using (var stream = File.OpenRead(filePath))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    files.CreateObject(ForumServer.Instance.Photos.PhotosContainer, stream, photo.Id.ToString());
                                    Console.WriteLine(@"Migrated photo: " + filename1600);
                                }
                            }
                            catch (Exception ex)
                            {
                                // remove the photo from the database and stop processing
                                using (var db = new ForumContext())
                                {
                                    var dbPhoto = await db.Photos.SingleAsync(q => q.Id.Equals(photo.Id));
                                    db.Photos.Remove(dbPhoto);
                                    await db.SaveChangesAsync();
                                }

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($@"Couldn't upload photo! Removed photo from db. File: {filename1600}. Exception: " + ex.Message);
                                Console.ResetColor();

                                if (!ex.Message.Contains("429")) continue;
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(@"Rate-limited by OVH! Pausing for 30secs.");
                                Console.ResetColor();
                                Thread.Sleep(TimeSpan.FromSeconds(30));
                            }
                            #endregion
                        }
                    }
                }
            }
        }

        private static async Task MigrateGalleriesAsync()
        {
            var category = ForumServer.Instance.Categories.Categories.Single(q => q.Name.Equals(CategoryName));
            var user = await ForumServer.Instance.Users.GetUserByUsernameAsync("londonbikers.com");
            using (
                var connection =
                    new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    // query for published galleries that we haven't already migrated
                    command.CommandText = $@"SELECT 
	                    g.ID, 
	                    g.f_title, 
	                    g.f_description, 
	                    g.f_creation_date,
	                    gc.f_name
	                    FROM [londonbikers_v5].[dbo].[apollo_galleries] g
	                    INNER JOIN [londonbikers_v5].[dbo].[apollo_gallery_category_gallery_relations] gcj ON gcj.GalleryID = g.ID
	                    INNER JOIN [londonbikers_v5].[dbo].[apollo_gallery_categories] gc ON gc.ID = gcj.CategoryID
	                    LEFT OUTER JOIN {TargetDatabaseName}.dbo.Posts p ON p.LegacyGalleryId = g.ID
	                    WHERE f_status = 1 AND p.Id IS NULL";
                    command.Connection = connection;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var legacyGalleryId = (long)reader["ID"];
                            var subject = reader["f_title"].ToString();
                            var content = reader["f_description"].ToString();
                            var created = (DateTime)reader["f_creation_date"];
                            var forumName = reader["f_name"].ToString();
                            var forum = category.Forums.Single(q => q.Name.Equals(forumName));

                            var topic = new Post
                            {
                                Created = created,
                                UserId = user.Id,
                                Subject = subject,
                                ForumId = forum.Id,
                                LegacyGalleryId = legacyGalleryId
                            };

                            if (!string.IsNullOrEmpty(content))
                                topic.Content = content;

                            // get the photo ids for this gallery
                            using (
                                var photoIdsConnection =
                                    new SqlConnection(
                                        ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                            using (
                                var photoIdsCommand =
                                    new SqlCommand(
                                        $"SELECT p.Id FROM [londonbikers_v5].[dbo].[GalleryImages] gi INNER JOIN {TargetDatabaseName}.dbo.Photos p ON p.LegacyGalleryPhotoId = gi.ID WHERE GalleryID = {legacyGalleryId} ORDER BY p.Created",
                                        photoIdsConnection))
                            {
                                photoIdsConnection.Open();
                                using (var photoIdsReader = await photoIdsCommand.ExecuteReaderAsync())
                                    while (await photoIdsReader.ReadAsync())
                                        topic.PhotoIdsToIncludeOnCreation.Add((Guid)photoIdsReader["Id"]);
                            }

                            await ForumServer.Instance.Posts.UpdatePostAsync(topic);
                            Console.WriteLine(@"Migrated gallery: " + topic.Subject);
                        }
                    }
                }
            }
        }

        private static async Task MigrateGalleryCommentsAsync()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var command = new SqlCommand())
            {
                connection.Open();
                command.Connection = connection;
                command.CommandText = $@"SELECT 
	                c.[ID] AS [LegacyCommentId],
	                c.[Created],
	                c.[Comment],
	                u.Id AS [LBV6UserId],
	                p.Id AS [LBV6TopicId]
	                FROM [londonbikers_v5].[dbo].[Comments] c
	                INNER JOIN {TargetDatabaseName}.dbo.AspNetUsers u ON u.LegacyApolloId = c.AuthorID
	                INNER JOIN {TargetDatabaseName}.dbo.Posts p ON p.LegacyGalleryId = c.OwnerID
	                LEFT OUTER JOIN {TargetDatabaseName}.dbo.Posts r ON r.[LegacyCommentId] = c.ID
	                WHERE c.OwnerType = 1 
                    AND c.[Status] = 1 
                    AND r.Id IS NULL";

                using (var reader = await command.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        var legacyCommentId = (long)reader["LegacyCommentId"];
                        var userId = reader["LBV6UserId"].ToString();
                        var created = (DateTime)reader["Created"];
                        var content = reader["Comment"].ToString();
                        var topicId = (long)reader["LBV6TopicId"];

                        var reply = new Post
                        {
                            Created = created,
                            Content = content,
                            PostId = topicId,
                            UserId = userId,
                            LegacyCommentId = legacyCommentId
                        };

                        await ForumServer.Instance.Posts.UpdatePostAsync(reply);
                        Console.WriteLine(@"Migrated gallery comment: " + reply.Content);
                    }
            }
        }

        private static async Task MigratePhotoCommentsAsync()
        {
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $@"SELECT 
	                    c.ID, 
	                    c.Created, 
	                    c.Comment, 
	                    u.Id AS [LBV6UserId],
	                    p.Id as [LBV6PostId],
	                    ph.Id as [LBV6PhotoId]
	                    FROM [londonbikers_v5].[dbo].[Comments] c
	                    INNER JOIN [londonbikers_v5].[dbo].GalleryImages gi ON gi.ID = c.OwnerID
	                    INNER JOIN {TargetDatabaseName}.dbo.AspNetUsers u ON u.LegacyApolloId = c.AuthorID
	                    INNER JOIN {TargetDatabaseName}.dbo.Posts p ON p.LegacyGalleryId = gi.GalleryID
	                    INNER JOIN {TargetDatabaseName}.dbo.Photos ph ON ph.LegacyGalleryPhotoId = gi.ID
	                    LEFT OUTER JOIN {TargetDatabaseName}.dbo.PhotoComments pc ON pc.LegacyCommentId = c.ID
	                    WHERE c.OwnerType = 2 
	                    AND c.[Status] = 1
	                    AND pc.Id IS NULL
                        ORDER BY c.Created";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var legacyCommentId = (long)reader["ID"];
                            var userId = reader["LBV6UserId"].ToString();
                            var created = (DateTime)reader["Created"];
                            var text = reader["Comment"].ToString();
                            var postId = (long)reader["LBV6PostId"];
                            var photoId = (Guid)reader["LBV6PhotoId"];

                            var pc = new PhotoComment
                            {
                                Created = created,
                                Text = text,
                                UserId = userId,
                                LegacyCommentId = legacyCommentId,
                                PhotoId = photoId
                            };

                            await ForumServer.Instance.Photos.UpdatePhotoCommentAsync(pc, postId);
                            Console.WriteLine($@"Migrated gallery {postId} comment: " +
                                              (text.Length > 100 ? text.Substring(0, 100) : text));
                        }
                    }
                }
            }
        }

        private static async Task UpdateForumStatsAsync()
        {
            using (var db = new ForumContext())
            {
                foreach (var f in _category.Forums)
                {
                    f.PostCount = await db.Posts.CountAsync(q => q.ForumId.HasValue && q.ForumId.Value.Equals(f.Id) && q.Status == PostStatus.Active);
                    f.LastUpdated = (await db.Posts.Where(q => q.ForumId.HasValue && q.ForumId.Value.Equals(f.Id) && q.Status == PostStatus.Active).OrderByDescending(q => q.Created).FirstAsync()).Created;
                    await ForumServer.Instance.Forums.UpdateForumAsync(f);
                }
            }

            Console.WriteLine(@"Updated forum stats.");
        }

        private static async Task RemoveOrphanedPhotoRecordsAsync()
        {
            var ids = new List<Guid>();
            var files = ForumServer.Instance.Photos.GetFilesProvider();
            using (var db = new ForumContext())
            {
                var photoIds = db.Photos.Where(q => q.LegacyGalleryPhotoId.HasValue).Select(q => q.Id);
                foreach (var photoId in photoIds)
                {
                    Console.WriteLine(@"Checking for orphaned record: " + photoId);

                    // do we have a matching file in the object-store?
                    try
                    {
                        // ReSharper disable once UnusedVariable
                        var md = files.GetObjectMetaData(ForumServer.Instance.Photos.PhotosContainer, photoId.ToString());
                    }
                    catch (ItemNotFoundException)
                    {
                        ids.Add(photoId);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($@"Photo {photoId} not found in store!");
                        Console.ResetColor();
                    }
                }

                foreach (var id in ids)
                {
                    db.Photos.Remove(await db.Photos.SingleAsync(q => q.Id.Equals(id)));
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(@"Removed orphaned photo: " + id);
                    Console.ResetColor();
                }

                await db.SaveChangesAsync();
                var photos = Convert.ToDouble(photoIds.Count());
                var percent = 100D / photos * Convert.ToDouble(ids.Count);
                Console.WriteLine($@"Removed {ids.Count} db records. That's {percent}%");
            }
        }

        private static async Task RemoveEmptyGalleriesAsync()
        {
            var count = 0;
            using (var db = new ForumContext())
            {
                foreach (var dbPost in db.Posts.Where(q => q.LegacyGalleryId.HasValue && !q.Photos.Any()))
                {
                    count++;
                    dbPost.Status = PostStatus.Removed;
                }
                    
                await db.SaveChangesAsync();
            }

            Console.WriteLine($@"Removed {count} empty galleries.");
        }

        private static async Task ActivateAllGalleriesAsync()
        {
            var count = 0;
            using (var db = new ForumContext())
            {
                foreach (var dbPost in db.Posts.Where(q => q.LegacyGalleryId.HasValue && q.Status == PostStatus.Removed))
                {
                    count++;
                    dbPost.Status = PostStatus.Active;
                }

                await db.SaveChangesAsync();
            }

            Console.WriteLine($@"Activated {count} removed galleries.");
        }
    }
}