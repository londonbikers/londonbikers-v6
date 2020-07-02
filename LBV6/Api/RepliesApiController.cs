using System;
using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Containers;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using StackExchange.Profiling;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LBV6.Api
{
    public class RepliesApiController : ApiController
    {
        // GET: api/replies/GetRepliesForTopic?topicId=x&limit=y&startIndex=z
        public async Task<IHttpActionResult> GetRepliesForTopic(long topicId, int limit, int startIndex)
        {
            // hard limit the number of headers that can be retrieved
            if (limit > 50)
                limit = 50;

            PostsContainer container;
            var profiler = MiniProfiler.Current;
            using (profiler.Step("Getting replies"))
            {
                container = User.Identity.IsAuthenticated ? 
                    await ForumServer.Instance.Posts.GetRepliesForTopicAsync(topicId, limit, startIndex, User.Identity.GetUserId()) : 
                    await ForumServer.Instance.Posts.GetRepliesForTopicAsync(topicId, limit, startIndex);
            }

            if (container == null)
                return NotFound();

            List<ReplyDto> replies;
            using (profiler.Step("DT"))
                replies = await Transformations.ConvertRepliesToReplyDtosAsync(container.Posts, ForumServer.Instance.Users.GetUserAsync);

            var response = new { Posts = replies, container.TotalPosts };
            return Ok(response);
        }

        // POST: api/replies/moderator_remove
        [HttpPost]
        [CheckModelForNull]
        [Authorize(Roles = "Moderator")]
        [Route("api/replies/moderator_remove")]
        public async Task<IHttpActionResult> ModeratorRemove(PostModerationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await ForumServer.Instance.Posts.ModeratorRemoveReplyAsync(dto.PostId, User.Identity.GetUserId(), dto.Reason);
            if (result)
                return Ok();

            // if we got this far then something bad happened.
            return InternalServerError();
        }

        // POST: api/replies
        [Authorize]
        [CheckModelForNull]
        public async Task<IHttpActionResult> Post([FromBody]ReplyDto replyDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reply = Transformations.ConvertReplyDtoToPost(replyDto);
            if (reply.Id < 1 && reply.PostId.HasValue)
            {
                // override the created date - don't trust clients, some get it wrong, some people might be trying to manipulate posts
                reply.Created = DateTime.UtcNow;
                await ForumServer.Instance.Posts.UpdatePostAsync(reply);
                var topic = await ForumServer.Instance.Posts.GetTopicAsync(reply.PostId.Value);

                // return the new total number of replies the parent topic has in case there's been
                // more replies since the client got its count. this is necessary for the UI to page through 
                // results to show the new reply in addition it needs to await this create as this create would 
                // increase the reply count too
                return Ok(new
                {
                    TopicReplyCount = topic.Replies.Count(r => r.Status != PostStatus.Removed),
                    ReplyId = reply.Id
                });
            }

            // this is an update request
            await ForumServer.Instance.Posts.UpdatePostAsync(reply);
            return Ok();
        }
    }
}