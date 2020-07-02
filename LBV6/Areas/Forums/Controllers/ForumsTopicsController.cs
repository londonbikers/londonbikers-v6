using LBV6.Models;
using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace LBV6.Areas.Forums.Controllers
{
    public class ForumsTopicsController : Controller
    {
        // GET: forums/posts/5/i-love-my-bike
        public async Task<ActionResult> Topic(long topicId)
        {
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(topicId);
            if (topic == null || topic.PostId.HasValue)
                return Redirect("/forums");

            if (topic.Status == PostStatus.Removed && topic.ForumId.HasValue)
            {
                Helpers.SetEventMessage("Sorry", "That topic no longer exists");
                var forum = ForumServer.Instance.Forums.GetForum(topic.ForumId.Value);
                return Redirect(Urls.GetForumUrl(forum));
            }

            // general page variables
            ViewBag.Title = topic.Subject;
            ViewBag.TopicId = topicId;
            ViewBag.Description = Utilities.GetContentSynopsis(topic.Content);

            #region open-graph
            var photos = new List<SharingImage>();
            if (topic.Photos != null)
                photos.AddRange(topic.Photos.Select(photo => new SharingImage { Url = Utilities.GetUrlForPhoto(photo), Width = photo.Width, Height = photo.Height }));

            if (topic.Attachments != null)
                photos.AddRange(topic.Attachments.Select(attachment => new SharingImage { Url = Utilities.GetPostAttachmentUrl(attachment) }));

            foreach (var reply in topic.Replies.Where(q => q.Status != PostStatus.Removed))
            {
                if (reply.Photos != null)
                    photos.AddRange(reply.Photos.Select(photo => new SharingImage { Url = Utilities.GetUrlForPhoto(photo), Width = photo.Width, Height = photo.Height }));

                if (reply.Attachments != null)
                    photos.AddRange(reply.Attachments.Select(attachment => new SharingImage { Url = Utilities.GetPostAttachmentUrl(attachment) }));
            }
            
            // take the first image we find in the thread.
            // todo: this could be improved by using View statistics to find the most popular image.
            var sharingImage = photos.FirstOrDefault();

            // Facebook open graph variables
            if (Request.Url != null) ViewBag.OgUrl = Request.Url.AbsoluteUri;
            ViewBag.OgTitle = topic.Subject;
            ViewBag.OgDescription = ViewBag.Description;

            if (sharingImage != null)
            {
                ViewBag.OgImage = sharingImage.Url;
                if (sharingImage.Width > 0)
                    ViewBag.OgImageWidth = sharingImage.Width.ToString();
                if (sharingImage.Height > 0)
                    ViewBag.OgImageHeight = sharingImage.Height.ToString();
            }
            #endregion

            return View("TopicTemplated");
        }

        /// <summary>
        /// This is a total hack, for some reason when doing a window.location redirect in JS, the page JS wasn't called, so the gallery wouldn't load.
        /// </summary>
        public ActionResult ConvertGalleryUrl(string url)
        {
            var uri = new Uri(url);
            if (uri.Host != ConfigurationManager.AppSettings["LB.Domain"])
                return RedirectToAction("Error", "Home");

            Helpers.Telemetry.TrackEvent("Device Gallery Conversion", new Dictionary<string, string> { { "Url", url } });
            return Redirect(url);
        }
    }
}