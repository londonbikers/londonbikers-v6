using LBV6Library;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.Web.Sitemap;

namespace SitemapGenerator
{
    public class Program
    {
        public static async Task Main()
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var pages = new List<Page>();

            AddStaticPages(pages);
            await AddForumStructurePagesAsync(pages);
            await AddForumPostPagesAsync(pages);
            await AddUserProfilePagesAsync(pages);

            CreateSiteMap(pages);

            stopWatch.Stop();
            Console.WriteLine($"Completed task in: {stopWatch.Elapsed }");
            
            //Console.WriteLine("Press any key to exit...");
            //Console.ReadKey();
        }

        private static async Task AddForumPostPagesAsync(ICollection<Page> pages)
        {
            Console.WriteLine("Adding topics & replies...");

            try
            {
                var pageSize = int.Parse(ConfigurationManager.AppSettings["LB.DefaultPageSize"]);

                // for now, set it to a small amount so we can debug easily
                //pageSize = 3;

                #region get the topic data
                using (var outerConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                using (var innerConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    outerConnection.Open();
                    innerConnection.Open();

                    using (var outerCommand = new SqlCommand())
                    using (var innerCommand = new SqlCommand())
                    {
                        outerCommand.Connection = outerConnection;
                        outerCommand.CommandText = @"select
	                        t.id as topic_id,
	                        t.[subject] as topic_subject,
	                        coalesce(t.editedon, t.created) as topic_last_updated
	                        from posts t
	                        where 
	                        t.ParentPost_Id is null and
	                        t.[status] = 0 and
	                        (select count(0) from ForumAccessRoles far where far.Forum_Id = t.Forum_Id) = 0";

                        innerCommand.Connection = innerConnection;
                        using (var topicReader = await outerCommand.ExecuteReaderAsync())
                        {
                            while (await topicReader.ReadAsync())
                            {
                                // new page, populate, assuming there are no replies
                                var page = new Page
                                {
                                    TopicId = topicReader["topic_id"].ToString(),
                                    Url = GetTopicUrl(topicReader["topic_id"].ToString(), topicReader["topic_subject"].ToString(), null),
                                    LastUpdated = (DateTime)topicReader["topic_last_updated"]
                                };

                                // now get the replies, if any for this topic
                                innerCommand.CommandText = $@"select
	                                r.id as reply_id,
	                                coalesce(r.editedon, r.created) as reply_last_updated
	                                from posts r
	                                where 
	                                r.ParentPost_Id = {topicReader["topic_id"]} and
	                                r.[status] = 0 
	                                order by r.id";

                                using (var replyReader = await innerCommand.ExecuteReaderAsync())
                                {
                                    if (!replyReader.HasRows)
                                    {
                                        // there's no replies, stop and complete the page
                                        pages.Add(page);
                                    }
                                    else
                                    {
                                        // there are replies, build additional pages
                                        // copy the row data into a set of replies to make it easier to work out how many pages we need to generate
                                        var replies = new List<Reply>();
                                        while (await replyReader.ReadAsync())
                                            replies.Add(new Reply { Id = replyReader["reply_id"].ToString(), LastUpdated = (DateTime)replyReader["reply_last_updated"] });

                                        // split the replies into chunks of smaller replies (i.e. page it)
                                        var pagedReplies = SplitList(replies, pageSize);
                                        var pageNumber = 1;

                                        foreach (var replyPage in pagedReplies)
                                        {
                                            page = new Page { TopicId = topicReader["topic_id"].ToString() };
                                            page.Url = GetTopicUrl(page.TopicId, topicReader["topic_subject"].ToString(), pageNumber);
                                            page.LastUpdated = replyPage.Last().LastUpdated;
                                            pages.Add(page);
                                            pageNumber++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                #endregion

                //foreach (var page in pages)
                //    Console.WriteLine("page: " + page.Url + ", last_updated: " + page.LastUpdated);

                Console.WriteLine($"Made {pages.Count:N0} pages.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task AddForumStructurePagesAsync(ICollection<Page> pages)
        {
            Console.WriteLine("Adding forum structure pages...");
            var categories = new List<Category>();

            #region get models
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "select id, name from categories";

                    // build category models
                    using (var categoriesReader = await command.ExecuteReaderAsync())
                    {
                        while (await categoriesReader.ReadAsync())
                            categories.Add(new Category { Id = categoriesReader["id"].ToString(), Name = categoriesReader["name"].ToString(), Forums = new List<Forum>() });
                    }

                    // build forum models
                    command.CommandText = @"select
	                    f.id,
	                    f.name,
	                    f.category_id
	                    from forums f
	                    left join ForumAccessRoles far on far.Forum_Id = f.Id
	                    where far.Id is null";

                    using (var forumsReader = await command.ExecuteReaderAsync())
                    {
                        while (await forumsReader.ReadAsync())
                        {
                            var category = categories.Single(q => q.Id.Equals(forumsReader["category_id"].ToString()));
                            category.Forums.Add(new Forum { Id = forumsReader["id"].ToString(), Name = forumsReader["name"].ToString() });
                        }
                    }

                    // remove empty categories (one's that contain non-public forums)
                    categories.RemoveAll(q => q.Forums.Count == 0);
                }
            }
            #endregion

            // add category and forum pages
            var baseUrl = ConfigurationManager.AppSettings["LB.Url"];
            foreach (var category in categories)
            {
                pages.Add(new Page { Url = $"{baseUrl}/categories/{category.Id}/{Utilities.EncodeText(category.Name)}" });

                foreach (var forum in category.Forums)
                    pages.Add(new Page { Url = $"{baseUrl}/forums/{forum.Id}/{Utilities.EncodeText(forum.Name)}" });
            }
        }

        private static void AddStaticPages(ICollection<Page> pages)
        {
            Console.WriteLine("Adding static pages...");
            var baseUrl = ConfigurationManager.AppSettings["LB.Url"];

            pages.Add(new Page { Url = baseUrl + "/forums" });
            pages.Add(new Page { Url = baseUrl + "/forums/poular" });
            pages.Add(new Page { Url = baseUrl + "/forums/browse" });
            pages.Add(new Page { Url = baseUrl + "/intercom" });
            pages.Add(new Page { Url = baseUrl + "/rules" });
            pages.Add(new Page { Url = baseUrl + "/privacy" });
            pages.Add(new Page { Url = baseUrl + "/contact" });
            pages.Add(new Page { Url = baseUrl + "/change" });
        }

        private static async Task AddUserProfilePagesAsync(ICollection<Page> pages)
        {
            Console.WriteLine("Adding user profile pages...");
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "select username from aspnetusers where status = 0";

                    // build username list
                    using (var usersReader = await command.ExecuteReaderAsync())
                        while (await usersReader.ReadAsync())
                            pages.Add(new Page { Url =  $"{ConfigurationManager.AppSettings["LB.Url"]}{Urls.GetUserProfileUrl(usersReader["username"].ToString())}"});
                }
            }
        }

        private static void CreateSiteMap(IReadOnlyCollection<Page> pages)
        {
            var sitemap = new Sitemap();
            sitemap.AddRange(pages.Select(page => new Url { Location = page.Url, TimeStamp = page.LastUpdated }));

            var sitemapGenerator = new X.Web.Sitemap.SitemapGenerator();
            var targetSitemapDirectory = new DirectoryInfo(ConfigurationManager.AppSettings["SitemapGenerator:OutputPath"]);
            sitemapGenerator.GenerateSitemaps(sitemap, targetSitemapDirectory);

            // generate one or more sitemaps (depending on the number of URLs) in the designated location.
            var fileInfoForGeneratedSitemaps = sitemapGenerator.GenerateSitemaps(sitemap, targetSitemapDirectory);

            var sitemapInfos = new List<SitemapInfo>();
            var dateSitemapWasUpdated = pages.Max(q => q.LastUpdated);

            foreach (var fileInfo in fileInfoForGeneratedSitemaps)
            {
                var uriToSitemap = new Uri($"{ConfigurationManager.AppSettings["LB.URL"]}{ConfigurationManager.AppSettings["SitemapGenerator:WebPath"]}/{fileInfo.Name}");
                sitemapInfos.Add(new SitemapInfo(uriToSitemap, dateSitemapWasUpdated));
            }

            // now generate the sitemap index file which has a reference to all of the sitemaps that were generated. 
            var sitemapIndexGenerator = new SitemapIndexGenerator();
            sitemapIndexGenerator.GenerateSitemapIndex(sitemapInfos, targetSitemapDirectory, "sitemap-index.xml");
        }

        private static string GetTopicUrl(string topicId, string subject, int? pageNumber)
        {
            var url = $"{ConfigurationManager.AppSettings["LB.Url"]}/forums/posts/{topicId}/{Utilities.EncodeText(subject)}";
            if (pageNumber.HasValue && pageNumber.Value > 1)
                url += "&p=" + pageNumber;

            return url;
        }

        private static IEnumerable<List<T>> SplitList<T>(List<T> locations, int pageSize)
        {
            for (var i = 0; i < locations.Count; i += pageSize)
                yield return locations.GetRange(i, Math.Min(pageSize, locations.Count - i));
        }
    }

    public class Reply
    {
        public string Id { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class Category
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Forum> Forums { get; set; }

    }

    public class Forum
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class Page
    {
        /// <summary>
        /// Used just to keep track of unfinished pages
        /// </summary>
        public string TopicId { get; set; }
        public string Url { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}