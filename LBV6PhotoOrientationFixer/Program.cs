using LBV6ForumApp;
using LBV6Library;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;

namespace LBV6PhotoOrientationFixer
{
    internal class Program
    {
        private static void Main()
        {
            // enumerate all photos and then work out correct orientation and update dimensions
            var files = ForumServer.Instance.Files.GetFilesProvider();
            using (var db = new ForumContext())
            {
                foreach (var dbReadPhoto in db.Photos.ToList())
                {
                    using (var stream = new MemoryStream())
                    {
                        files.GetObject(ConfigurationManager.AppSettings["OVH.OpenStack.FileContainer"], dbReadPhoto.Id.ToString(), stream);

                        try
                        {
                            using (var image = Image.FromStream(stream))
                            {
                                Console.WriteLine($"Inspecting image: {dbReadPhoto.Id}...");
                                var size = Utilities.GetAutoRotatedImageDimensions(image);
                                var dbUpdatePhoto = db.Photos.Single(q => q.Id.Equals(dbReadPhoto.Id));
                                if (dbUpdatePhoto.Width == size.Width) continue;

                                Console.BackgroundColor = ConsoleColor.Red;
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"Dimensions not the same! Updating from {dbUpdatePhoto.Width} x {dbUpdatePhoto.Height} to {size.Width} x {size.Height}");
                                Console.ResetColor();

                                dbUpdatePhoto.Width = size.Width;
                                dbUpdatePhoto.Height = size.Height;
                            }
                        }
                        catch (ArgumentException)
                        {
                            Console.WriteLine($"ArgumentException thrown for photo id: {dbReadPhoto.Id}");
                        }
                    }
                }

                db.SaveChanges();
            }

            Console.WriteLine(@"-------------------------------------------------");
            Console.WriteLine(@"All done. Press any key to quit.");
            Console.ReadKey(true);
        }
    }
}