using LBV6ForumApp;
using LBV6Library;
using LBV6Library.Exceptions;
using LBV6Library.Models;
using LBV6Library.Models.Dtos;
using Microsoft.AspNet.Identity;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;

namespace LBV6.Api
{
    [Authorize]
    public class IntercomApiController : ApiController
    {
        #region members
        private const string UserNotAuthorisedForHeader = "The user is not authorised to perform operations against this private message header.";
        #endregion

        [HttpGet]
        public async Task<IHttpActionResult> GetHeaders(int limit, int startIndex, bool filterUnread)
        {
            if (limit > 50)
                limit = 50;

            var userId = User.Identity.GetUserId();
            var headers = await ForumServer.Instance.Messages.GetHeadersAsync(userId, limit, startIndex, filterUnread);
            var dtos = await Transformations.ConvertPrivateMessageHeadersToPrivateMessageHeaderDtosAsync(headers, userId, ForumServer.Instance.Users.GetUserAsync);
            return Ok(dtos);
        }

        /// <summary>
        /// If a header already exists between the current user and a specified one then that header will be
        /// returned. If not, a NotFound() response will be returned.
        /// </summary>
        [HttpGet]
        public async Task<IHttpActionResult> GetHeaderToSingleUser(string userId)
        {
            var currentUserId = User.Identity.GetUserId();
            var header = await ForumServer.Instance.Messages.GetHeaderForTwoUsers(currentUserId, userId);
            if (header == null)
                return NotFound();

            var dto = await Transformations.ConvertPrivateMessageHeaderToPrivateMessageHeaderDtoAsync(header, currentUserId, ForumServer.Instance.Users.GetUserAsync);
            return Ok(dto);
        }

        [HttpPost]
        [CheckModelForNull]
        public async Task<IHttpActionResult> CreateMessageHeader([FromBody]PrivateMessageHeaderDto dto)
        {
            if (dto.Id > 0)
                return BadRequest("CreateMessageHeader has already been created");

            if (dto.Users.Count > int.Parse(ConfigurationManager.AppSettings["LB.Intercom.MaxHeaderUserCount"]))
                return BadRequest("Cannot create a header with that many users in.");

            if (dto.Users.Count == 1)
                return BadRequest("Private message header must have more than one user in.");

            if (!dto.Users.Any(q => q.User.Id.Equals(User.Identity.GetUserId())))
                return BadRequest("Private message header user list does not contain current user.");

            var header = Transformations.ConvertPrivateMessageHeaderDtoToPrivateMessageHeader(dto);
            await ForumServer.Instance.Messages.CreateHeaderAsync(header);
            return Ok(header.Id);
        }

        [HttpPost]
        public async Task<IHttpActionResult> AddMesageHeaderUser(long headerId, string userId)
        {
            var header = await ForumServer.Instance.Messages.GetHeaderAsync(headerId);
            if (header == null)
                return NotFound();

            // the current user has to be in the list of header users to perform this operation.
            if (!header.Users.Any(q => q.UserId.Equals(User.Identity.GetUserId())))
                return BadRequest(UserNotAuthorisedForHeader);

            // don't add duplicates
            if (header.Users.Any(q => q.UserId.Equals(userId)))
                return BadRequest("That user is already a member of the private message header.");

            await ForumServer.Instance.Messages.AddUserToHeaderAsync(header, userId);
            return Ok();
        }

        [HttpPost]
        public async Task<IHttpActionResult> RemoveMesageHeaderUser(long headerId, string userId)
        {
            var header = await ForumServer.Instance.Messages.GetHeaderAsync(headerId);
            if (header == null)
                return NotFound();

            // validation: the current user has to be in the list of header users to perform this operation.
            var currentUserId = User.Identity.GetUserId();
            if (!header.Users.Any(q => q.UserId.Equals(currentUserId)))
                return BadRequest(UserNotAuthorisedForHeader);

            var operationType = currentUserId.Equals(userId)
                ? PrivateMessageUserOperationType.UserRemovedSelf
                : PrivateMessageUserOperationType.UserWasRemoved;

            await ForumServer.Instance.Messages.RemoveUserFromHeaderAsync(header, userId, operationType);
            return Ok();
        }

        [HttpPost]
        [CheckModelForNull]
        public async Task<IHttpActionResult> CreateMessage([FromBody]PrivateMessageDto dto)
        {
            if (dto.Id > 0)
                return BadRequest("PrivateMessage has already been created.");

            var message = Transformations.ConvertPrivateMessageDtoToPrivateMessage(dto);

            // don't trust user id and created values from the client
            message.UserId = User.Identity.GetUserId();
            message.Created = DateTime.UtcNow;

            try
            {
                await ForumServer.Instance.Messages.CreateMessageAsync(message);
                return Ok(message.Id);
            }
            catch (NotAuthorisedException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //todo: review
        [HttpPost]
        public async Task<IHttpActionResult> MarkMessageAsRead(long messageId)
        {
            var userId = User.Identity.GetUserId();
            try
            {
                await ForumServer.Instance.Messages.MarkMessageAsReadAsync(messageId, userId);
                return Ok();
            }
            catch (NotAuthorisedException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //todo: review
        [HttpPost]
        public async Task<IHttpActionResult> MarkAllMessagesInHeaderAsRead(long headerId)
        {
            var userId = User.Identity.GetUserId();
            try
            {
                await ForumServer.Instance.Messages.MarkAllMessagesInHeaderAsReadAsync(headerId, userId);
                return Ok();
            }
            catch (NotAuthorisedException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //todo: review
        [HttpPost]
        public async Task<IHttpActionResult> MarkEveryMessageAsRead()
        {
            var userId = User.Identity.GetUserId();
            try
            {
                await ForumServer.Instance.Messages.MarkEveryMessageAsReadAsync(userId);
                return Ok();
            }
            catch (NotAuthorisedException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetMessages(long headerId, int limit, int startIndex, bool markAsRead)
        {
            if (limit > 50)
                limit = 50;

            var userId = User.Identity.GetUserId();
            try
            {
                var messages = await ForumServer.Instance.Messages.GetMessagesAsync(headerId, userId, limit, startIndex, markAsRead);
                var dtos = await Transformations.ConvertPrivateMessagesToPrivateMessageDtosAsync(messages, ForumServer.Instance.Users.GetUserAsync);
                return Ok(dtos);
            }
            catch (NotAuthorisedException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}