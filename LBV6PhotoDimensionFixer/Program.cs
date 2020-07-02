using System.Configuration;
using System.IO;
using System.Drawing;
using System.Linq;
using LBV6ForumApp;

namespace LBV6PhotoDimensionFixer
{
    internal class Program
    {
        private static void Main()
        {
            // enumerate all photos in db without dimensions
            var files = ForumServer.Instance.Files.GetFilesProvider();
            using (var db = new ForumContext())
            {
                foreach (var dbPhoto in db.Photos.Where(q => !q.Width.HasValue).ToList())
                {
                    using (var stream = new MemoryStream())
                    {
                        files.GetObject(ConfigurationManager.AppSettings["OVH.OpenStack.FileContainer"], dbPhoto.Id.ToString(), stream);
                        using (var image = Image.FromStream(stream))
                        {
                            var dbPhoto2 = db.Photos.Single(q => q.Id.Equals(dbPhoto.Id));
                            dbPhoto2.Width = image.Width;
                            dbPhoto2.Height = image.Height;
                        }
                    }
                }

                db.SaveChanges();
            }
        }
    }
}