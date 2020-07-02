using LBV6ForumApp;
using LBV6Library.Models;
using net.openstack.Core.Exceptions.Response;
using System;
using System.Configuration;
using System.IO;
using System.Linq;

namespace ProfilePhotoMigrator
{
    internal class Program
    {
        private static void Main()
        {
            // enumerate all users who have a profile photo
            var files = ForumServer.Instance.Files.GetFilesProvider();
            using (var db = new ForumContext())
            {
                foreach (var dbUser in db.Users.Where(q => q.ProfilePhotoVersion.HasValue).ToList())
                {
                    if (!dbUser.ProfilePhotoVersion.HasValue)
                        continue;

                    var path = GetUserProfilePictureLocation(dbUser);
                    var fileStoreId = $"{dbUser.Id}_{dbUser.ProfilePhotoVersion.Value}";
                    var fileMigrated = true;

                    try
                    {
                        files.GetObjectMetaData("Profiles", fileStoreId);
                    }
                    catch (ItemNotFoundException)
                    {
                        fileMigrated = false;
                    }

                    if (fileMigrated)
                    {
                        Console.WriteLine(@"File already migrated: " + path);
                    }
                    else if (File.Exists(path))
                    {
                        using (var memoryStream = new MemoryStream(File.ReadAllBytes(path)))
                        {
                            files.CreateObject("Profiles", memoryStream, $"{dbUser.Id}_{dbUser.ProfilePhotoVersion.Value}");
                            Console.WriteLine(@"Migrated file: " + path);
                        }
                    }
                    else
                    {
                        Console.WriteLine(@"File not found: " + path);
                    }
                }
            }

            Console.WriteLine(@"Press any key to exit.");
            Console.ReadKey(true);
        }

        /// <summary>
        /// Returns the local path to the folder where the user's profile photo is or is to be stored.
        /// </summary>
        public static string GetUserProfilePictureLocation(User user)
        {
            return !user.ProfilePhotoVersion.HasValue ? null : $"{ConfigurationManager.AppSettings["LB.MediaRoot"]}\\{user.Created.Year}\\{user.Created.Month}\\{user.Created.Day}\\{user.Id}_{user.ProfilePhotoVersion.Value}.jpg";
        }
    }
}