using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using StackExchange.Profiling;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace LBV6.Api
{
    public class ForumsApiController : ApiController
    {
        // GET: api/forums/5
        [ResponseType(typeof(ForumDto))]
        public IHttpActionResult Get(long id)
        {
            var profiler = MiniProfiler.Current;
            Forum forum;

            using (profiler.Step("Get forum"))
            {
                // convert our domain forum to dto
                forum = ForumServer.Instance.Forums.GetForum(id);
                if (forum == null)
                    return NotFound();
            }

            using (profiler.Step("Is user authorised?"))
            {
                // is the user authorised to access this forum?
                if (!Helpers.CanUserAccessForum(forum))
                    return Unauthorized();
            }

            using (profiler.Step("Transform"))
            {
                var forumDto = Transformations.ConvertForumToForumDto(forum, ForumServer.Instance.Categories.Categories);
                return Ok(forumDto);
            }
        }

        // POST: api/forums
        [Authorize]
        [CheckModelForNull]
        [ResponseType(typeof(ForumDto))]
        public async Task<IHttpActionResult> Post([FromBody]ForumDto forumDto)
        {
            var forum = Transformations.ConvertForumDtoToForum(forumDto);
            Validate(forum);
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            forumDto.Id = forum.Id;
            return Ok(forumDto);
        }

        // PUT: api/forums
        [Authorize]
        [CheckModelForNull]
        public async Task<IHttpActionResult> Put([FromBody]ForumDto forumDto)
        {
            var forum = Transformations.ConvertForumDtoToForum(forumDto);
            Validate(forum);
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            return Ok();
        }

        // DELETE: api/forums/5
        [Authorize]
        public async Task Delete(long id)
        {
            await ForumServer.Instance.Forums.DeleteForumAsync(id);
        }

        // POST: api/forums/AddForumAccessRole
        [Authorize]
        [CheckModelForNull]
        [HttpPost]
        public async Task<IHttpActionResult> AddForumAccessRole([FromBody] ForumRoleDto dto)
        {
            var role = Transformations.ConvertForumRoleDtoToForumAccessRole(dto);
            var forum = ForumServer.Instance.Forums.GetForum(dto.ForumId);
            forum.AccessRoles.Add(role);
            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            return Ok(Transformations.ConvertForumAccessRoleToForumRoleDto(role));
        }

        // DELETE: api/forums/AddForumAccessRole
        [Authorize]
        [CheckModelForNull]
        [HttpDelete]
        public async Task<IHttpActionResult> RemoveForumAccessRole([FromBody] ForumRoleDto dto)
        {
            var role = Transformations.ConvertForumRoleDtoToForumAccessRole(dto);
            var forum = ForumServer.Instance.Forums.GetForum(dto.ForumId);
            forum.AccessRoles.RemoveAll(q => q.Id.Equals(role.Id));
            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            return Ok();
        }

        [Authorize]
        [CheckModelForNull]
        [HttpPost]
        public async Task<IHttpActionResult> AddForumPostRole([FromBody] ForumRoleDto dto)
        {
            var role = Transformations.ConvertForumRoleDtoToForumPostRole(dto);
            var forum = ForumServer.Instance.Forums.GetForum(dto.ForumId);
            forum.PostRoles.Add(role);
            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            return Ok(Transformations.ConvertForumPostRoleToForumRoleDto(role));
        }

        // DELETE: api/forums/AddForumAccessRole
        [Authorize]
        [CheckModelForNull]
        [HttpDelete]
        public async Task<IHttpActionResult> RemoveForumPostRole([FromBody] ForumRoleDto dto)
        {
            var role = Transformations.ConvertForumRoleDtoToForumPostRole(dto);
            var forum = ForumServer.Instance.Forums.GetForum(dto.ForumId);
            forum.PostRoles.RemoveAll(q => q.Id.Equals(role.Id));
            await ForumServer.Instance.Forums.UpdateForumAsync(forum);
            return Ok();
        }

        // GET: api/forums/is_subscribed
        [HttpGet]
        [Authorize]
        [Route("api/forums/is_subscribed")]
        public async Task<bool> IsUserSubscribedToForum(long id)
        {
            return await ForumServer.Instance.Notifications.IsUserSubscribedToForumAsync(User.Identity.GetUserId(), id);
        }

        // POST: api/forums/subscribe
        [HttpPost]
        [Authorize]
        [Route("api/forums/subscribe")]
        public async Task<IHttpActionResult> SubscribeToForum(long id)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            var forum = ForumServer.Instance.Forums.GetForum(id);
            await ForumServer.Instance.Notifications.SubscribeToForumAsync(user, forum);
            return Ok();
        }

        // POST: api/forums/unsubscribe
        [HttpPost]
        [Authorize]
        [Route("api/forums/unsubscribe")]
        public async Task<IHttpActionResult> UnSubscribeFromForum(long id)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(User.Identity.GetUserId());
            var forum = ForumServer.Instance.Forums.GetForum(id);
            await ForumServer.Instance.Notifications.UnSubscribeFromForumAsync(user, forum);
            return Ok();
        }
    }
}