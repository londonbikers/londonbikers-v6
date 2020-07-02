using LBV6ForumApp;
using LBV6Library;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace LBV6.Api
{
    public class LegacyApiController : ApiController
    {
        [ResponseType(typeof(string))]
        public IHttpActionResult GetRandomPopularGalleryImage()
        {
            // get from cache
            var filenames = (List<string>)HttpContext.Current.Cache.Get("GetRandomPopularGalleryImage");
            if (filenames == null)
            {
                // not in cache, load from database
                using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                using (var command = new SqlCommand("", connection))
                {
                    command.CommandText = @"SELECT TOP 1000 GI.Filename1600
	                FROM [londonbikers_v5].[dbo].[GalleryImages] GI
	                INNER JOIN [londonbikers_v5].[dbo].[apollo_gallery_category_gallery_relations] R ON GI.GalleryID = R.GalleryID
	                INNER JOIN [londonbikers_v5].[dbo].[apollo_gallery_categories] C ON C.ID = R.CategoryID
	                WHERE 
		                GI.Filename1600 IS NOT NULL AND 
		                GI.Filename1600 <> '' AND 
		                C.f_name IN ('Racing', 'Featured Bikes', 'Motorcycles', 'Articles', 'Trackdays', 'Ride-outs') AND
		                GI.Name NOT LIKE ' caterpillar ' AND
		                GI.Name NOT LIKE ' car ' AND
		                GI.Name NOT LIKE ' cars ' AND
		                GI.Name NOT LIKE ' truck ' AND
		                GI.Name NOT LIKE ' trucks ' AND
		                GI.Name NOT LIKE ' Nissan GTR ' AND
		                GI.Name NOT LIKE 'Nissan GTR'
	                ORDER BY NEWID()";

                    connection.Open();
                    filenames = new List<string>();
                    using (var reader = command.ExecuteReader())
                        while (reader.Read())
                            filenames.Add((string)reader[0]);
                }

                // cache the results
                HttpContext.Current.Cache.Insert("GetRandomPopularGalleryImage", filenames);
            }

            // return a random filename
            var filename = filenames[new Random().Next(0, filenames.Count)];

            // build the URL
            filename = ConfigurationManager.AppSettings["LB.GalleriesMediaUrl"] + "/1600/" + filename;

            return Ok(filename);
        }

        public async Task<IHttpActionResult> LegacyTopicRedirect(int legacyTopicId)
        {
            var topic = await ForumServer.Instance.Posts.GetTopicByLegacyIdAsync(legacyTopicId);
            var topicUrl = Urls.GetTopicUrl(topic, true);
            Helpers.Telemetry.TrackEvent("Legacy topic redirect", new Dictionary<string, string> { { "LegacyTopicId", legacyTopicId.ToString() } });
            HttpContext.Current.Response.RedirectPermanent(topicUrl, false);
            return Ok();
        }

        public IHttpActionResult LegacyDefaultRedirect()
        {
            var url = ConfigurationManager.AppSettings["LB.Url"] + "/change";
            Helpers.Telemetry.TrackEvent("Legacy default redirect");
            return Redirect(url);
        }
    }
}