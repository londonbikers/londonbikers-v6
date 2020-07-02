using net.openstack.Core.Domain;
using net.openstack.Core.Providers;
using net.openstack.Providers.Rackspace;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace ObjectStoreDownloader2
{
    internal class Program
    {
        private static CloudFilesProvider CloudFilesProvider { get; set; }

        private static void Main()
        {
            // objectives:
            // download all files from OVH that are referenced in Photos table
            // download all files from OVH that are referenced in Users table (cover and avatars)
            // download all files from OVH that are referenced in PrivateMessageAttachments table

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            CloudFilesProvider = GetCloudFilesProvider();

            DownloadCoverPhotos();
            DownloadProfilePhotos();
            DownloadPhotos();
            DownloadPrivateMessagePhotos();

            stopwatch.Stop();
            Console.WriteLine($"Program took {stopwatch.Elapsed.ToString()} to complete.");

            var activityFilePath = Directory.GetCurrentDirectory() + @"\last_ran.txt";
            File.WriteAllText(activityFilePath, DateTime.Now.ToString(CultureInfo.InvariantCulture));
        }

        private static void DownloadPhotos()
        {
            // do we have a point to pick up from?
            var startCreated = DateTime.Parse("01/01/2000");
            var deltaFilePath = Directory.GetCurrentDirectory() + @"\photos_delta.txt";
            if (File.Exists(deltaFilePath))
                startCreated = new DateTime(long.Parse(File.ReadAllText(deltaFilePath)));

            var lastCreated = DateTime.MinValue;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Source"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"SELECT Id, Created, ContentType FROM Photos WHERE Created > '{startCreated:yyyy-MM-dd HH:mm:ss.fff}' ORDER BY Created";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var created = (DateTime)reader["Created"];
                            var id = (Guid)reader["Id"];
                            var mimeType = (string)reader["ContentType"];
                            var folder = $@"{ConfigurationManager.AppSettings["PhotosBasePath"]}\{created.Year}\{created.Month}\{created.Day}";
                            var filename = $"{id}{MimeTypes.GetFileExtension(mimeType)}";

                            try
                            {
                                Directory.CreateDirectory(folder);
                                CloudFilesProvider.GetObjectSaveToFile("Primary", folder, id.ToString(), filename);
                                Console.WriteLine($"Downloaded Post Photo: {filename}");
                                lastCreated = created;
                            }
                            catch (Exception)
                            {
                                // oops, crash, make sure we keep a note of where we stopped
                                if (lastCreated != DateTime.MinValue)
                                    File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());

                                throw;
                            }
                        }
                    }
                }
            }

            Console.WriteLine();

            // save the created date of the last file we downloaded so we don't have to download them all again when we re-run the program
            if (lastCreated != DateTime.MinValue)
                File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());
        }

        private static void DownloadProfilePhotos()
        {
            // do we have a point to pick up from?
            var startCreated = DateTime.Parse("01/01/2000");
            var deltaFilePath = Directory.GetCurrentDirectory() + @"\profile_photos_delta.txt";
            if (File.Exists(deltaFilePath))
                startCreated = new DateTime(long.Parse(File.ReadAllText(deltaFilePath)));

            var lastCreated = DateTime.MinValue;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Source"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"select Id, Created, ProfilePhotoVersion from aspnetusers where ProfilePhotoVersion is not null and created > '{startCreated:yyyy-MM-dd HH:mm:ss.fff}' order by created";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var created = (DateTime)reader["Created"];
                            var id = (string)reader["Id"];
                            var version = (int)reader["ProfilePhotoVersion"];
                            var folder = $@"{ConfigurationManager.AppSettings["ProfilePhotosBasePath"]}\{created.Year}\{created.Month}\{created.Day}";
                            var filename = $"{id}_{version}.jpg";

                            try
                            {
                                Directory.CreateDirectory(folder);
                                CloudFilesProvider.GetObjectSaveToFile("Profiles", folder, $"{id}_{version}", filename);
                                Console.WriteLine($"Downloaded Profile Photo: {filename}");
                                lastCreated = created;
                            }
                            catch (Exception)
                            {
                                // oops, crash, make sure we keep a note of where we stopped
                                if (lastCreated != DateTime.MinValue)
                                    File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());

                                throw;
                            }
                        }
                    }
                }
            }

            Console.WriteLine();

            // save the id of the last file we downloaded so we don't have to download them all again when we re-run the program
            if (lastCreated != DateTime.MinValue)
                File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());
        }

        private static void DownloadCoverPhotos()
        {
            // do we have a point to pick up from?
            var startCreated = DateTime.Parse("01/01/2000");
            var deltaFilePath = Directory.GetCurrentDirectory() + @"\cover_photos_delta.txt";
            if (File.Exists(deltaFilePath))
                startCreated = new DateTime(long.Parse(File.ReadAllText(deltaFilePath)));

            var lastCreated = DateTime.MinValue;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Source"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"select created, CoverPhotoContentType, CoverPhotoId from aspnetusers where coverphotoid is not null and created > '{startCreated:yyyy-MM-dd HH:mm:ss.fff}' order by created";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var created = (DateTime)reader["Created"];
                            var id = (Guid)reader["CoverPhotoId"];
                            var mimeType = (string)reader["CoverPhotoContentType"];
                            var folder = $@"{ConfigurationManager.AppSettings["CoverPhotosBasePath"]}\{created.Year}\{created.Month}\{created.Day}";
                            var filename = $"{id}{MimeTypes.GetFileExtension(mimeType)}";

                            try
                            {
                                Directory.CreateDirectory(folder);
                                CloudFilesProvider.GetObjectSaveToFile("Covers", folder, id.ToString(), filename);
                                Console.WriteLine($"Downloaded Cover Photo: {filename}");
                                lastCreated = created;
                            }
                            catch (Exception)
                            {
                                // oops, crash, make sure we keep a note of where we stopped
                                if (lastCreated != DateTime.MinValue)
                                    File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());

                                throw;
                            }
                        }
                    }
                }
            }

            Console.WriteLine();

            // save the id of the last file we downloaded so we don't have to download them all again when we re-run the program
            if (lastCreated != DateTime.MinValue)
                File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());
        }

        private static void DownloadPrivateMessagePhotos()
        {
            // do we have a point to pick up from?
            var startCreated = DateTime.Parse("01/01/2000");
            var deltaFilePath = Directory.GetCurrentDirectory() + @"\pm_photos_delta.txt";
            if (File.Exists(deltaFilePath))
                startCreated = new DateTime(long.Parse(File.ReadAllText(deltaFilePath)));

            var lastCreated = DateTime.MinValue;
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Source"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = $"select created, contenttype, filestoreid from PrivateMessageAttachments where created > '{startCreated:yyyy-MM-dd HH:mm:ss.fff}' order by created";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var created = (DateTime)reader["Created"];
                            var id = (Guid)reader["filestoreid"];
                            var mimeType = (string)reader["contenttype"];
                            var folder = $@"{ConfigurationManager.AppSettings["PrivateMessagePhotosBasePath"]}\{created.Year}\{created.Month}\{created.Day}";
                            var filename = $"{id}{MimeTypes.GetFileExtension(mimeType)}";

                            try
                            {
                                Directory.CreateDirectory(folder);
                                CloudFilesProvider.GetObjectSaveToFile("PrivateMessagePhotos", folder, id.ToString(), filename);
                                Console.WriteLine($"Downloaded Private Message Photo: {filename}");
                                lastCreated = created;
                            }
                            catch (Exception)
                            {
                                // oops, crash, make sure we keep a note of where we stopped
                                if (lastCreated != DateTime.MinValue)
                                    File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());

                                throw;
                            }
                        }
                    }
                }
            }

            Console.WriteLine();

            // save the id of the last file we downloaded so we don't have to download them all again when we re-run the program
            if (lastCreated != DateTime.MinValue)
                File.WriteAllText(deltaFilePath, lastCreated.Ticks.ToString());
        }

        private static CloudFilesProvider GetCloudFilesProvider()
        {
            // http://www.openstacknetsdk.org
            var urlBase = new Uri("https://auth.cloud.ovh.net/v2.0");
            var cloudIdentityWithProject = new CloudIdentityWithProject
            {
                Password = ConfigurationManager.AppSettings["OVH.OpenStack.Password"],
                Username = ConfigurationManager.AppSettings["OVH.OpenStack.Username"],
                ProjectName = ConfigurationManager.AppSettings["OVH.OpenStack.ProjectName"]
            };

            var identityProvider = new OpenStackIdentityProvider(urlBase, cloudIdentityWithProject);
            var cloudFilesProvider = new CloudFilesProvider(null, ConfigurationManager.AppSettings["OVH.OpenStack.ProjectRegion"], identityProvider, null);
            return cloudFilesProvider;
        }
    }
}
