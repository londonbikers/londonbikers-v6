using LBV6Library;
using LBV6Library.Exceptions;
using LBV6Library.Models;
using Microsoft.AspNet.SignalR.Hubs;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace LBV6ForumApp.Controllers
{
    public class PrivateMessageController
    {
        #region accessors
        /// <summary>
        /// Enables the controller to send updates to Intercom objects to clients connected to the web-app, in real-time.
        /// </summary>
        private static IHubConnectionContext<dynamic> IntercomClients { get; set; }
        #endregion

        #region constructors
        internal PrivateMessageController()
        {
        }
        #endregion

        #region dependency registration
        /// <summary>
        /// For the controller to send intercom updates to web-app clients in real-time, the web-app must register the SignalR hub with the controller. Do this at startup.
        /// </summary>
        public void RegisterClients(IHubConnectionContext<dynamic> clients)
        {
            if (IntercomClients != null)
                throw new InvalidOperationException("IHubConnectionContext already specified.");

            IntercomClients = clients;
        }
        #endregion

        #region public methods
        public async Task<PrivateMessageHeader> GetHeaderAsync(long headerId)
        {
            using (var db = new ForumContext())
                return await db.PrivateMessageHeaders.Include("Users").SingleOrDefaultAsync(q => q.Id.Equals(headerId));
        }

        public async Task<List<PrivateMessageHeader>> GetHeadersAsync(string userId, int limit, int startIndex, bool filterUnread)
        {
            using (var db = new ForumContext())
            {
                if (filterUnread)
                {
                    // just return headers for the user that have unread messages in
                    return await db.PrivateMessageHeaders.Include("Users").
                        Where(q =>
                            q.Users.Any(u => u.UserId.Equals(userId)) &&
                            q.Users.FirstOrDefault(w => w.UserId.Equals(userId)).HasDeleted == null &&
                            q.Users.FirstOrDefault(w => w.UserId.Equals(userId)).HasUnreadMessages).
                        OrderByDescending(q => q.LastMessageCreated).
                        Skip(startIndex).
                        Take(limit).ToListAsync();
                }

                // default header retrieval for user
                return await db.PrivateMessageHeaders.Include("Users").
                    Where(q => q.Users.Any(u => u.UserId.Equals(userId)) && q.Users.FirstOrDefault(w => w.UserId.Equals(userId)).HasDeleted == null).
                    OrderByDescending(q => q.LastMessageCreated).
                    Skip(startIndex).
                    Take(limit).ToListAsync();
            }
        }

        public async Task<PrivateMessageHeader> GetHeaderForTwoUsers(string user1Id, string user2Id)
        {
            using (var db = new ForumContext())
            {
                return await db.PrivateMessageHeaders.Include(q => q.Users).Where(q =>
                    q.Users.Count == 2 && q.Users.Any(w => w.UserId.Equals(user1Id)) &&
                    q.Users.Any(r => r.UserId.Equals(user2Id)) && q.Users.All(r => r.HasDeleted == null))
                    .OrderByDescending(q => q.Id)
                    .FirstOrDefaultAsync();
            }
        }

        public async Task AddUserToHeaderAsync(PrivateMessageHeader header, string userId)
        {
            using (var db = new ForumContext())
            {
                db.PrivateMessageHeaderUsers.Add(new PrivateMessageHeaderUser { UserId = userId, PrivateMessageHeaderId = header.Id });
                await db.SaveChangesAsync();
            }

            // also add a system message to indicate to the other recipients that a user has been added
            var systemMessage = new PrivateMessage
            {
                PrivateMessageHeaderId = header.Id,
                Type = PrivateMessageType.UserAdded,
                UserId = userId
            };

            await CreateMessageAsync(systemMessage);
        }

        public async Task RemoveUserFromHeaderAsync(PrivateMessageHeader header, string userId, PrivateMessageUserOperationType operationType)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (header.Id < 1)
                throw new ArgumentException("header hasn't been persisted yet.");

            // how many recipients are there? 
            // 1: if there's only one recipient (if there's another they should be marked as HasDeleted) then this is a full blown deletion scenario.
            // 2: if there's only two then the user is actually deleting the thread not being removed from it entirely. this allows the last recipient to still message that user.

            if (header.Users.Count(q => !q.HasDeleted.HasValue) == 1)
            {
                // there's nobody left to view the messages so delete the whole header dependency tree
                await DeleteHeader(header);
                ForumServer.Instance.Telemetry.TrackEvent("Intercom: Header deleted");
                return;
            }

            using (var db = new ForumContext())
            {
                var dbHeaderUser = db.PrivateMessageHeaderUsers.Single(q => q.PrivateMessageHeaderId.Equals(header.Id) && q.UserId.Equals(userId));

                if (header.Users.Count == 2)
                {
                    // there's still a recipient left after this so mark the user as having deleted the conversation
                    // leaving it open for the other person to still message them.
                    dbHeaderUser.HasDeleted = true;
                    dbHeaderUser.Added = DateTime.UtcNow;
                    ForumServer.Instance.Telemetry.TrackEvent("Intercom: User marked as deleted");
                }
                else
                {
                    // there's more than two users, so a group chat - remove the user
                    db.PrivateMessageHeaderUsers.Remove(dbHeaderUser);

                    // also add a system message to indicate to the other recipients that a user has been removed
                    var systemMessage = new PrivateMessage
                    {
                        PrivateMessageHeaderId = header.Id,
                        Type = operationType == PrivateMessageUserOperationType.UserRemovedSelf ? PrivateMessageType.UserLeft : PrivateMessageType.UserRemoved,
                        UserId = userId
                    };

                    await CreateMessageAsync(systemMessage);
                    ForumServer.Instance.Telemetry.TrackEvent("Intercom: User removed from header");
                }

                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Creates new headers. Does not add or remove users, for that use the dedicated AddUserToHeaderAsync() and RemoveUserFromHeaderAsync() methods.
        /// </summary>
        public async Task CreateHeaderAsync(PrivateMessageHeader header)
        {
            if (header == null)
                throw new ArgumentNullException(nameof(header));

            if (header.Id > 0)
                throw new ArgumentException("Header is already persisted.");

            using (var db = new ForumContext())
            {
                // don't create system add-messages when creating a header
                foreach (var headerUser in header.Users)
                {
                    // don't trust client creation dates
                    headerUser.Added = DateTime.UtcNow;
                    db.PrivateMessageHeaderUsers.Add(headerUser);
                }

                db.PrivateMessageHeaders.Add(header);
                ForumServer.Instance.Telemetry.TrackEvent("Intercom: New header created");
                await db.SaveChangesAsync();
            }

            // todo: how should we implement intercom caching?
        }

        /// <summary>
        /// Creates a new private message. Note: messages cannot be edited - they are immutable.
        /// </summary>
        public async Task CreateMessageAsync(PrivateMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (message.PrivateMessageHeaderId < 1)
                throw new ArgumentException("Message header doesn't look to have been persisted yet.");

            if (message.Id > 0)
                throw new ArgumentException("Message looks already to have been created.");

            if (message.Type == PrivateMessageType.Message && string.IsNullOrEmpty(message.Content))
                throw new ArgumentException("User messages must have content.");

            using (var db = new ForumContext())
            {
                // validation
                if (!await DoesUserBelongToHeaderAsync(message.PrivateMessageHeaderId, message.UserId, db))
                    throw new NotAuthorisedException("The user is not authorised to perform operations against this private message header.");

                // create the message
                db.PrivateMessages.Add(message);
                await db.SaveChangesAsync();
                ForumServer.Instance.Telemetry.TrackEvent("Intercom: Message created");
            }

            // perform background post-processing that doesn't have to be done before we hand back
            BackgroundTaskScheduler.QueueBackgroundWorkItem(async ctx => await PostProcessCreateMessageAsync(message));

            // todo: how could we implement intercom caching?
        }

        /// <summary>
        /// Gets all the messages for a user for a message header and pages the results.
        /// </summary>
        public async Task<List<PrivateMessage>> GetMessagesAsync(long headerId, string userId, int limit, int startIndex, bool markAsRead)
        {
            using (var db = new ForumContext())
            {
                // validate that the user is a part of the header
                var userAdded = await db.PrivateMessageHeaderUsers.Where(q => q.UserId.Equals(userId) && q.PrivateMessageHeaderId.Equals(headerId)).Select(q => q.Added).SingleOrDefaultAsync();
                if (userAdded == DateTime.MinValue)
                    throw new NotAuthorisedException("The user is not authorised to perform operations against this private message header.");

                var messages = await db.PrivateMessages.Include("ReadBy").Include("Attachments").
                    Where(q => q.PrivateMessageHeaderId.Equals(headerId) && q.Created >= userAdded).
                    OrderByDescending(q => q.Created).
                    Skip(startIndex).
                    Take(limit).ToListAsync();

                if (markAsRead)
                    BackgroundTaskScheduler.QueueBackgroundWorkItem(async ct => await MarkMessagesAsReadAsync(headerId, messages.Select(q => q.Id), userId));

                return messages;
            }
        }

        /// <summary>
        /// Marks a message as being read by a private message header user.
        /// </summary>
        public async Task MarkMessageAsReadAsync(long privateMessageId, string userId, bool validateUserIsInHeader = true)
        {
            using (var db = new ForumContext())
            {
                // validation
                if (validateUserIsInHeader)
                    if (!await CanUserMarkMessageAsRead(privateMessageId, userId, db))
                        throw new NotAuthorisedException("The user is not authorised to mark this message as read.");

                // check that we don't already have a read record for this message and user combo.
                if (db.PrivateMessageReadBys.Any(q => q.PrivateMessageId.Equals(privateMessageId) && q.UserId.Equals(userId)))
                    return;

                db.PrivateMessageReadBys.Add(new PrivateMessageReadBy { PrivateMessageId = privateMessageId, UserId = userId });
                await db.SaveChangesAsync();

                // update unread stats
                var headerId = db.PrivateMessages.Single(q => q.Id.Equals(privateMessageId)).PrivateMessageHeaderId;
                await UpdateHeaderUserHasUnreadMessages(headerId, userId);
                await SetUserUnreadMessageCountAsync(userId);

                // reduce the notifications accordingly
                await ForumServer.Instance.Notifications.InvalidateSingleMessageNotificationAsync(userId, headerId);
            }
        }

        /// <summary>
        /// Allows a user to mark all the messages in a thread that they didn't author as read.
        /// </summary>
        public async Task MarkAllMessagesInHeaderAsReadAsync(long headerId, string userId, bool updateUserUnreadMessageCount = true, bool sendHeaderUpdatesToWebClient = true)
        {
            // task:
            // go over each message the user didn't author after the time they joined the discussion
            // ensure each has a read-by
            // ensure the header user is marked as having no unread messages

            using (var db = new ForumContext())
            {
                // validate that the user is a part of the header
                var userAdded = await db.PrivateMessageHeaderUsers.Where(q => q.UserId.Equals(userId) && q.PrivateMessageHeaderId.Equals(headerId)).Select(q => q.Added).SingleOrDefaultAsync();
                if (userAdded == DateTime.MinValue)
                    throw new NotAuthorisedException("The user is not authorised to perform operations against this private message header.");

                // get a list of messages ids in the header the user didn't create and from after they were added to the header
                foreach (var dbMessageId in db.PrivateMessages.Include(q => q.ReadBy).Where(q =>
                      q.PrivateMessageHeaderId.Equals(headerId) &&
                      !q.UserId.Equals(userId) &&
                      q.Type == PrivateMessageType.Message &&
                      q.Created >= userAdded &&
                      !q.ReadBy.Any(w => w.UserId.Equals(userId))).Select(q => q.Id))
                {
                    // no read by for this user - create it
                    db.PrivateMessageReadBys.Add(new PrivateMessageReadBy
                    {
                        PrivateMessageId = dbMessageId,
                        UserId = userId,
                        When = DateTime.UtcNow
                    });
                }

                var dbHeaderUser = await db.PrivateMessageHeaderUsers.SingleAsync(q => q.UserId.Equals(userId) && q.PrivateMessageHeaderId.Equals(headerId));
                dbHeaderUser.HasUnreadMessages = false;
                await db.SaveChangesAsync();

                // do we need to update the header view model on the client side?
                // we don't want to run this for bulk operations, i.e. mark every message as read as it would push out too much unecessary data to the client.
                if (sendHeaderUpdatesToWebClient)
                {
                    var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(headerId));
                    await SendPrivateMessageHeaderToWebClientAsync(dbHeader, userId);
                }
            }

            if (updateUserUnreadMessageCount)
                await SetUserUnreadMessageCountAsync(userId);

            // reduce the notifications accordingly
            await ForumServer.Instance.Notifications.InvalidateAllMessageHeaderNotificationsAsync(userId, headerId);
        }

        /// <summary>
        /// Allows a user to mark all messages that they didn't author in all headers they're a part of as read.
        /// </summary>
        public async Task MarkEveryMessageAsReadAsync(string userId)
        {
            using (var db = new ForumContext())
            {
                // get a list of the headers the user is a part of
                foreach (var headerId in
                    await db.PrivateMessageHeaders
                    .Include(q => q.Users)
                    .Where(q => q.Users.Any(w => w.UserId.Equals(userId)) && q.Users.FirstOrDefault(w => w.UserId.Equals(userId)).HasUnreadMessages)
                    .Select(q => q.Id)
                    .ToListAsync())
                {
                    await MarkAllMessagesInHeaderAsReadAsync(headerId, userId, false, false);
                }
            }

            await SetUserUnreadMessageCountAsync(userId);
        }
        #endregion

        #region validation
        private static async Task<bool> DoesUserBelongToHeaderAsync(long headerId, string userId, ForumContext db)
        {
            return await db.PrivateMessageHeaderUsers.AnyAsync(q => q.PrivateMessageHeaderId.Equals(headerId) && q.UserId.Equals(userId));
        }

        private static async Task<bool> CanUserMarkMessageAsRead(long messageId, string userId, ForumContext db)
        {
            var headerId = (await db.PrivateMessages.SingleAsync(q => q.Id.Equals(messageId))).PrivateMessageHeaderId;
            return await DoesUserBelongToHeaderAsync(headerId, userId, db);
        }
        #endregion

        #region private methods
        /// <summary>
        /// Completely deletes a header and all dependent private message objects, i.e. messages, attachments etc.
        /// </summary>
        private static async Task DeleteHeader(PrivateMessageHeader header)
        {
            using (var db = new ForumContext())
            {
                // delete any attachment files in cloud storage
                var dbAttachments = db.PrivateMessageAttachments.Where(q => q.PrivateMessage.Id.Equals(header.Id));
                foreach (var dbAttachment in dbAttachments)
                {
                    try
                    {
                        var files = ForumServer.Instance.Photos.GetFilesProvider();
                        files.DeleteObject(ForumServer.Instance.Photos.PrivateMessagePhotosContainer, dbAttachment.FilestoreId.ToString());
                    }
                    catch (Exception ex)
                    {
                        Logging.LogError(typeof(PrivateMessageController).FullName, ex);
                    }
                }

                // delete all of the header dependencies
                db.PrivateMessageReadBys.RemoveRange(db.PrivateMessageReadBys.Where(q => q.PrivateMessageId.Equals(header.Id)));
                db.PrivateMessageAttachments.RemoveRange(dbAttachments);
                db.PrivateMessages.RemoveRange(db.PrivateMessages.Where(q => q.PrivateMessageHeaderId.Equals(header.Id)));
                db.PrivateMessageHeaderUsers.RemoveRange(db.PrivateMessageHeaderUsers.Where(q => q.PrivateMessageHeaderId.Equals(header.Id)));
                db.PrivateMessageHeaders.Remove(db.PrivateMessageHeaders.Single(q => q.Id.Equals(header.Id)));

                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Enables a group of messages to be marked as read by a specific user. 
        /// Does not perform validation to see if the user is able to read the message.
        /// Assumes all messages belong to the same header.
        /// </summary>
        private async Task MarkMessagesAsReadAsync(long headerId, IEnumerable<long> messageIds, string userId)
        {
            foreach (var messageId in messageIds)
                await MarkMessageAsReadAsync(messageId, userId, false);

            await UpdateHeaderUserHasUnreadMessages(headerId, userId);
            await SetUserUnreadMessageCountAsync(userId);
        }

        /// <summary>
        /// Looks to see if a user has any unread messages in a header and updates the header user record accordingly.
        /// </summary>
        private static async Task UpdateHeaderUserHasUnreadMessages(long headerId, string userId)
        {
            // does every message in the header from other people have a ready by for this user? 
            // if not, they have unread messages
            using (var db = new ForumContext())
            {
                var dbHeaderUser = await db.PrivateMessageHeaderUsers.SingleOrDefaultAsync(q => q.PrivateMessageHeaderId.Equals(headerId) && q.UserId.Equals(userId));

                var hasUnreadMessagesInHeader = db.PrivateMessages.Include(q => q.ReadBy).Any(q =>
                    q.PrivateMessageHeaderId.Equals(headerId) &&
                    !q.UserId.Equals(userId) &&
                    q.Type == PrivateMessageType.Message &&
                    q.Created >= dbHeaderUser.Added &&
                    !q.ReadBy.Any(w => w.UserId.Equals(userId)));

                if (dbHeaderUser.HasUnreadMessages != hasUnreadMessagesInHeader)
                {
                    dbHeaderUser.HasUnreadMessages = hasUnreadMessagesInHeader;
                    await db.SaveChangesAsync();

                    // update the users view models to let them know they have unread messages
                    var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(headerId));
                    await SendPrivateMessageHeaderToWebClientAsync(dbHeader, dbHeaderUser.UserId);
                }
            }
        }

        /// <summary>
        /// When a user reads a private message, a header is marked as being read or all their messages are being marked as read, 
        /// the unread message count property needs updating on the user object.
        /// </summary>
        private async Task SetUserUnreadMessageCountAsync(string userId)
        {
            var user = await ForumServer.Instance.Users.GetUserAsync(userId);
            var oldCount = user.UnreadMessagesCount;
            int newCount;

            using (var db = new ForumContext())
            {
                newCount = db.PrivateMessageHeaderUsers.Count(q => q.UserId.Equals(user.Id) && q.HasUnreadMessages);
                user.UnreadMessagesCount = newCount;
            }

            await ForumServer.Instance.Users.UpdateUserAsync(user);

            if (newCount != oldCount)
                OnUserUnreadMessageCountChange(EventArgs.Empty, user);
        }

        /// <summary>
        /// Performs tasks part of the create-message process that are not critical to returning to the user, i.e. secondary, background tasks.
        /// </summary>
        private async Task PostProcessCreateMessageAsync(PrivateMessage message)
        {
            try
            {
                if (message.Type != PrivateMessageType.Message)
                {
                    Logging.LogDebug(GetType().FullName, "PostProcessCreateMessageAsync: Quitting - not a user message");
                    return;
                }

                List<PrivateMessageHeaderUser> otherHeaderUsers;

                // update the header last-message-created timestamp if this is a user message
                using (var db = new ForumContext())
                {
                    var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleAsync(q => q.Id.Equals(message.PrivateMessageHeaderId));
                    dbHeader.LastMessageCreated = message.Created;

                    // update header user metadata for users other than the message author
                    otherHeaderUsers = await db.PrivateMessageHeaderUsers.Where(q => q.PrivateMessageHeaderId.Equals(message.PrivateMessageHeaderId) && q.UserId != message.UserId).ToListAsync();
                    foreach (var dbHeaderUser in otherHeaderUsers)
                    {
                        // update message stats for user
                        dbHeaderUser.HasUnreadMessages = true;

                        // when there are only two header users and one of them has marked the thread for deletion and it's no longer shown to them 
                        // then the other recipient can continue to send messages to this person and if they do we need to show the header to the
                        // other user again (though they will only see messages from that point).
                        if (dbHeaderUser.HasDeleted.HasValue && dbHeaderUser.HasDeleted.Value)
                            dbHeaderUser.HasDeleted = null;

                        // update the users view models to let them know they have unread messages
                        await SendPrivateMessageHeaderToWebClientAsync(dbHeader, dbHeaderUser.UserId);
                    }

                    await db.SaveChangesAsync();
                }

                foreach (var dbHeaderUser in otherHeaderUsers)
                {
                    await SetUserUnreadMessageCountAsync(dbHeaderUser.UserId);
                    await SendPrivateMessageToWebClientAsync(message, dbHeaderUser.UserId);
                }

                OnPrivateMessageCreated(EventArgs.Empty, message);
            }
            catch (Exception ex)
            {
                // catch here as we're on a background thread
                Logging.LogError(GetType().FullName, ex);
            }
        }

        /// <summary>
        /// Sends either new or updated private message headers to a specific header user so if they're connected, they can update their header view model.
        /// </summary>
        private static async Task SendPrivateMessageHeaderToWebClientAsync(PrivateMessageHeader header, string userId)
        {
            if (IntercomClients == null)
                return;

            var headerDto = await Transformations.ConvertPrivateMessageHeaderToPrivateMessageHeaderDtoAsync(header, userId, ForumServer.Instance.Users.GetUserAsync);
            IntercomClients.User(userId).updateHeader(headerDto);
        }

        private static async Task SendPrivateMessageToWebClientAsync(PrivateMessage message, string userId)
        {
            if (IntercomClients == null)
                return;

            var messageDto = await Transformations.ConvertPrivateMessageToPrivateMessageDtoAsync(message, ForumServer.Instance.Users.GetUserAsync);
            IntercomClients.User(userId).receiveMessage(messageDto);
        }
        #endregion

        #region events
        public delegate void PrivateMessageEventHandler(PrivateMessage privateMessage);
        public event PrivateMessageEventHandler PrivateMessageCreated;
        protected virtual void OnPrivateMessageCreated(EventArgs e, PrivateMessage privateMessage)
        {
            var handler = PrivateMessageCreated;
            handler?.Invoke(privateMessage);
        }

        public delegate void UserUnreadMessageCountChangedEventHandler(User user);
        public event UserUnreadMessageCountChangedEventHandler UserUnreadMessageCountChanged;
        protected virtual void OnUserUnreadMessageCountChange(EventArgs e, User user)
        {
            var handler = UserUnreadMessageCountChanged;
            handler?.Invoke(user);
        }
        #endregion
    }
}