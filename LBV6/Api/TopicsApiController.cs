using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Containers;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using StackExchange.Profiling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;

namespace LBV6.Api
{
    public class TopicsApiController : ApiController
    {
        // GET: api/topics/5
        [ResponseType(typeof(TopicDto))]
        public async Task<IHttpActionResult> Get(long id)
        {
            var post = await ForumServer.Instance.Posts.GetTopicAsync(id, User.Identity.GetUserId());
            if (post == null || post.PostId.HasValue || post.Status == PostStatus.Removed)
                return NotFound();

            RecordTopicView(post);

            // convert our domain post to a dto
            var topicDto = await Transformations.ConvertTopicToTopicDtoAsync(
                post, 
                ForumServer.Instance.Categories.Categories, 
                ForumServer.Instance.Users.GetUserAsync, 
                ForumServer.Instance.Forums.GetForum);

            return Ok(topicDto);
        }

        // GET: api/topics/GetHeader?topicId=5
        [HttpGet]
        [ResponseType(typeof(TopicHeaderDto))]
        public async Task<IHttpActionResult> GetHeader(long topicId)
        {
            var container = await ForumServer.Instance.Posts.GetTopicHeaderAsync(topicId, User.Identity.GetUserId());
            if (container == null)
                return NotFound();

            var headers = await Transformations.ConvertTopicsToTopicHeaderDtosAsync(container, User.Identity.IsAuthenticated, ForumServer.Instance.Users.GetUserAsync, ForumServer.Instance.Forums.GetForum);
            return Ok(headers[0]);
        }

        // GET: api/topics/GetHeadersForForum?forumid=x&limit=y&startindex=z
        public async Task<IHttpActionResult> GetHeadersForForum(long forumId, int limit, int startIndex)
        {
            // hard limit the number of headers that can be retrieved
            if (limit > 50)
                limit = 50;

            var container = await ForumServer.Instance.Posts.GetTopicsForForumAsync(forumId, limit, startIndex, User.Identity.GetUserId());
            if (container == null)
                return NotFound();

            var topicHeaders = await Transformations.ConvertTopicsToTopicHeaderDtosAsync(container, User.Identity.IsAuthenticated, ForumServer.Instance.Users.GetUserAsync, ForumServer.Instance.Forums.GetForum);
            var response = new { Headers = topicHeaders, TotalItems = container.TotalPosts };
            return Ok(response);
        }

        // GET: api/topics/GetHeadersForPopularTopics?days=x&limit=y&startindex=z
        public async Task<IHttpActionResult> GetHeadersForPopularTopics(int days, int limit, int startIndex)
        {
            // there seems to be a intermitent performance issue with large date ranges as though the first big query takes forever
            // and then subsequent requests are fine. a huge delay isn't acceptable so until we track down the cause (a perf trace shows
            // a huge amount of EF reference resolution calls being made) then we'll have to limit the number of days we query for
            // to avoid introducing a performance damper.
            if (days > 30)
                days = 30;

            // hard limit the number of headers that can be retrieved
            if (limit > 50)
                limit = 50;

            // some forums that popular topics can reside in are restricted so the app needs to know what roles the user has so it
            // can filter out posts from forums the user isn't authorised to view.
            var profiler = MiniProfiler.Current;
            var roles = new List<string>();
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                using (profiler.Step("Get roles"))
                {
                    // lame hard-coded shit due to not being able to get roles from ASPNET Identity here
                    if (User.IsInRole("Administrator"))
                        roles.Add("Administrator");
                    if (User.IsInRole("Moderator"))
                        roles.Add("Moderator");
                }
            }

            PostsContainer container;
            using (profiler.Step("Get topics"))
            {
                container = ForumServer.Instance.Posts.GetPopularTopics(TimeSpan.FromDays(days), limit, startIndex, User.Identity.GetUserId(), roles);
                if (container == null)
                    return NotFound();
            }

            using (profiler.Step("DT"))
            {
                var topicHeaders = await Transformations.ConvertTopicsToTopicHeaderDtosAsync(container, User.Identity.IsAuthenticated, ForumServer.Instance.Users.GetUserAsync, ForumServer.Instance.Forums.GetForum);
                var response = new { Headers = topicHeaders, TotalItems = container.TotalPosts };
                return Ok(response);
            }
        }

        // GET: api/topics/GetHeadersForLatestTopics?limit=x&startindex=y
        public async Task<IHttpActionResult> GetHeadersForLatestTopics(int limit, int startIndex)
        {
            // hard limit the number of headers that can be retrieved
            if (limit > 50)
                limit = 50;

            // some forums that latest topics can reside in are restricted so the app needs to know what roles the user has so it
            // can filter out posts from forums the user isn't authorised to view.
            var roles = new List<string>();
            if (HttpContext.Current.User.Identity.IsAuthenticated)
            {
                // lame hard-coded shit due to not being able to get roles from ASPNET Identity here
                if (User.IsInRole("Administrator"))
                    roles.Add("Administrator");

                if (User.IsInRole("Moderator"))
                    roles.Add("Moderator");
            }

            var container = ForumServer.Instance.Posts.GetLatestTopics(limit, startIndex, User.Identity.GetUserId(), roles);
            if (container == null)
                return NotFound();

            var topicHeaders = await Transformations.ConvertTopicsToTopicHeaderDtosAsync(
                container, 
                User.Identity.IsAuthenticated, 
                ForumServer.Instance.Users.GetUserAsync, 
                ForumServer.Instance.Forums.GetForum);

            var response = new { Headers = topicHeaders, TotalItems = container.TotalPosts };
            return Ok(response);
        }

        // POST: api/topics/moderator_remove
        [HttpPost]
        [CheckModelForNull]
        [Authorize(Roles = "Moderator")]
        [Route("api/topics/moderator_remove")]
        public async Task<IHttpActionResult> ModeratorRemove(PostModerationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var topic = await ForumServer.Instance.Posts.GetTopicAsync(dto.PostId);
            if (topic == null)
                return NotFound();

            await ForumServer.Instance.Posts.ModeratorRemoveTopicAsync(topic, User.Identity.GetUserId(), dto.Reason);
            return Ok();
        }

        // POST: api/topics/moderator_close
        [HttpPost]
        [CheckModelForNull]
        [Authorize(Roles = "Moderator")]
        [Route("api/topics/moderator_close")]
        public async Task<IHttpActionResult> ModeratorClose(PostModerationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var topic = await ForumServer.Instance.Posts.GetTopicAsync(dto.PostId);
            if (topic == null)
                return NotFound();

            await ForumServer.Instance.Posts.ModeratorCloseTopicAsync(topic, User.Identity.GetUserId(), dto.Reason);
            return Ok();
        }

        // POST: api/topics/moderator_move
        [HttpPost]
        [CheckModelForNull]
        [Authorize(Roles = "Moderator")]
        [Route("api/topics/moderator_move")]
        public async Task<IHttpActionResult> ModeratorMove(PostModerationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var topic = await ForumServer.Instance.Posts.GetTopicAsync(dto.PostId);
            if (topic == null)
                return NotFound();

            await ForumServer.Instance.Posts.ModeratorMoveTopicAsync(topic, dto.DestinationForumId, User.Identity.GetUserId(), dto.Reason);
            return Ok();
        }

        // POST: api/topics
        [Authorize]
        [HttpPost]
        [CheckModelForNull]
        [Route("api/topics")]
        public async Task<IHttpActionResult> Post([FromBody]TopicDto topicDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var topic = Transformations.ConvertTopicDtoToPost(topicDto);
            var forum = ForumServer.Instance.Forums.GetForum(topicDto.ForumId);

            if (!Helpers.CanUserPostToForum(forum))
                return BadRequest("User doesn't have permission to post to that forum!");

            if (topic.Id < 1)
            {
                // create the topic
                // override the created date - don't trust clients, some get it wrong, some people might be trying to manipulate posts, 
                // i.e. put them in the future to always be at the top of indexes.
                topic.Created = DateTime.UtcNow;

                // make sure we start using the new master version of the topic
                topic = await ForumServer.Instance.Posts.UpdatePostAsync(topic);

                // do we have a photo credit to apply to each photo?
                if (string.IsNullOrEmpty(topicDto.PhotoCredits))
                    return Ok(topic.Id);

                foreach (var photo in topic.Photos)
                {
                    photo.Credit = topicDto.PhotoCredits;
                    await ForumServer.Instance.Photos.UpdatePhotoAsync(topic, photo);
                }

                return Ok(topic.Id);
            }

            // this is an update request
            await ForumServer.Instance.Posts.UpdatePostAsync(topic);
            return Ok();
        }

        // GET: api/topics/is_subscribed
        [HttpGet]
        [Authorize]
        [Route("api/topics/is_subscribed")]
        public async Task<bool> IsUserSubscribedToTopic(long id)
        {
            return await ForumServer.Instance.Notifications.IsUserSubscribedToTopicAsync(User.Identity.GetUserId(), id);
        }

        // POST: api/topics/subscribe
        [HttpPost]
        [Authorize]
        [Route("api/topics/subscribe")]
        public async Task<IHttpActionResult> SubscribeToTopic(long id)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(id);
            await ForumServer.Instance.Notifications.SubscribeToTopicAsync(user, topic);
            return Ok();
        }

        // POST: api/topics/unsubscribe
        [HttpPost]
        [Authorize]
        [Route("api/topics/unsubscribe")]
        public async Task<IHttpActionResult> UnSubscribeFromTopic(long id)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            var topic = await ForumServer.Instance.Posts.GetTopicAsync(id);
            await ForumServer.Instance.Notifications.UnSubscribeFromTopicAsync(user, topic);
            return Ok();
        }

        #region private methods
        /// <summary>
        /// This will increase the view count for a topic each time it is viewed (requested), though only
        /// one view per sessions is recorded to avoid user-gaming of views.
        /// </summary>
        private static void RecordTopicView(Post topic)
        {
            if (topic == null)
                throw new ArgumentNullException(nameof(topic));

            if (topic.PostId.HasValue)
                throw new ArgumentException("Post is not a topic.");

            if (HttpContext.Current.Session["TopicViews"] == null)
                HttpContext.Current.Session["TopicViews"] = new List<long>();

            // check that the user hasn't already viewed the topic this session
            var views = (List<long>)HttpContext.Current.Session["TopicViews"];
            if (views.Any(q => q.Equals(topic.Id)))
                return;

            ForumServer.Instance.Posts.IncrementTopicViews(topic, HttpContext.Current.User.Identity.IsAuthenticated
                    ? HttpContext.Current.User.Identity.GetUserId()
                    : HttpContext.Current.Request.UserHostAddress);
        }
        #endregion
    }
}