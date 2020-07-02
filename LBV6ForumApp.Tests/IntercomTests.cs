using LBV6Library.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LBV6ForumApp.Tests
{
    [TestClass]
    public class IntercomTests
    {
        #region members
        //private const string User1 = "001fc906-a893-4c97-ae88-624bc0d01b26";
        //private const string User2 = "00205196-3e27-47de-86a6-1ba3eaa0fbec";
        //private const string User3 = "0022669d-36b5-4639-9a3c-c0e1e82dc713";
        //private const string User4 = "0086405d-aed3-4c3d-b043-39a19ed737cd";

        private const string User1 = "001ad6bd-4581-4bc5-b8b8-cd24ec0578db";
        private const string User2 = "001ca302-d8ab-424e-80af-f4581b2215ee";
        private const string User3 = "001fa9f1-7862-4023-afe7-e8ac3b8c9c9a";
        private const string User4 = "0020a516-20eb-4328-b119-aaebf5d27e94";
        #endregion

        [TestMethod]
        public async Task CreateMessageHeaderTest()
        {
            await CreatePrivateMessageHeaderAsync();
        }

        [TestMethod]
        public async Task CreatePrivateMessageTest()
        {
            await CreatePrivateMessageAsync();
        }

        [TestMethod]
        public async Task MarkMessageAsReadTest()
        {
            await MarkMessageAsReadAsync();
        }

        [TestMethod]
        public async Task MarkAllMessagesAsReadTest()
        {
            await MarkAllMessagesAsReadAsync();
        }

        [TestMethod]
        public async Task UserHasUnreadMessagesTest()
        {
            const string user = User2;
            await UserHasUnreadMessages(user: user);
        }

        [TestMethod]
        public async Task UserHasUnreadMessagesMultipleTest()
        {
            const string sender = User1;
            const string receiver = User2;
            var header = await CreatePrivateMessageHeaderAsync();
            var message1 = await CreatePrivateMessageAsync(header, sender);
            var message2 = await CreatePrivateMessageAsync(header, sender);
            var message3 = await CreatePrivateMessageAsync(header, sender);
            var message4 = await CreatePrivateMessageAsync(header, sender);
            var message5 = await CreatePrivateMessageAsync(header, sender);

            await MarkMessageAsReadAsync(receiver, message1);
            await MarkMessageAsReadAsync(receiver, message2);
            await MarkMessageAsReadAsync(receiver, message3);
            await MarkMessageAsReadAsync(receiver, message4);
            await MarkMessageAsReadAsync(receiver, message5);

            await UserHasUnreadMessages(header, receiver);
        }

        [TestMethod]
        public async Task AddUserTest()
        {
            var header = await CreatePrivateMessageHeaderAsync();
            await ForumServer.Instance.Messages.AddUserToHeaderAsync(header, User4);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(header.Id));

                Assert.IsNotNull(dbHeader);
                Assert.AreEqual(dbHeader.Users.Count, 4);
                Assert.IsTrue(dbHeader.Users.Any(q => q.UserId.Equals(User4)));
            }
        }

        [TestMethod]
        public async Task UserRemovedSelfTest()
        {
            var header = await CreatePrivateMessageHeaderAsync();
            await ForumServer.Instance.Messages.RemoveUserFromHeaderAsync(header, User3, PrivateMessageUserOperationType.UserRemovedSelf);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(header.Id));

                Assert.IsNotNull(dbHeader);
                Assert.AreEqual(dbHeader.Users.Count, 2);
                Assert.IsTrue(!dbHeader.Users.Any(q => q.UserId.Equals(User3)));
            }
        }

        [TestMethod]
        public async Task UserWasRemovedTest()
        {
            var header = await CreatePrivateMessageHeaderAsync();
            await ForumServer.Instance.Messages.RemoveUserFromHeaderAsync(header, User3, PrivateMessageUserOperationType.UserWasRemoved);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(header.Id));

                Assert.IsNotNull(dbHeader);
                Assert.AreEqual(dbHeader.Users.Count, 2);
                Assert.IsTrue(!dbHeader.Users.Any(q => q.UserId.Equals(User3)));
            }
        }

        [TestMethod]
        public async Task GetHeadersTest()
        {
            var headers = await ForumServer.Instance.Messages.GetHeadersAsync(User2, 25, 0, false);
            Console.WriteLine(@"headers.Count: " + headers.Count);
            Assert.IsNotNull(headers);
            Assert.AreEqual(25, headers.Count);
        }

        [TestMethod]
        public async Task GetHeaderTest()
        {
            const string sender = User1;
            const string recipient = User2;
            var header = await CreatePrivateMessageHeaderAsync();
            await CreatePrivateMessageAsync(header, sender);

            // make sure the header is in the headers list
            var headers = await ForumServer.Instance.Messages.GetHeadersAsync(recipient, 25, 0, false);

            Assert.IsNotNull(headers, "headers were null");
            Assert.IsNotNull(headers[0], "first header was null");
            Assert.AreEqual(headers[0].Id, header.Id, "first head ids are not equal");
            Assert.IsNotNull(headers[0].Users, "first header users is null");
            Assert.AreEqual(headers[0].Users.Count, 3, "first header user count is not 3");
            Assert.IsTrue(headers[0].Users.Any(q => q.UserId.Equals(recipient)), "first header users doesn't contain recipient");

            // make sure the header has unread messages for the recipient we're interested in
            var headerUser = headers[0].Users.Single(q => q.UserId.Equals(recipient));
            Assert.IsTrue(headerUser.HasUnreadMessages);
        }

        [TestMethod]
        public async Task UseCase1ReadMessageHeaderShowsCorrectReadStatusTest()
        {
            const string sender = User1;
            const string recipient = User2;
            var header = await CreatePrivateMessageHeaderAsync();
            var message = await CreatePrivateMessageAsync(header, sender);

            // make sure the header is in the headers list
            var headers = await ForumServer.Instance.Messages.GetHeadersAsync(recipient, 25, 0, false);
            Assert.IsNotNull(headers);
            Assert.IsNotNull(headers[0]);
            Assert.AreEqual(headers[0].Id, header.Id);
            Assert.IsNotNull(headers[0].Users);
            Assert.AreEqual(headers[0].Users.Count, 3);
            Assert.IsTrue(headers[0].Users.Any(q => q.UserId.Equals(recipient)));

            // make sure the header has unread messages for the recipient we're interested in
            var headerUser = headers[0].Users.Single(q => q.UserId.Equals(recipient));
            Assert.IsTrue(headerUser.HasUnreadMessages);

            // now mark the message as read
            await ForumServer.Instance.Messages.MarkMessageAsReadAsync(message.Id, recipient);

            // now get the header again and check the read status again
            var header2 = await ForumServer.Instance.Messages.GetHeaderAsync(header.Id);
            Assert.IsNotNull(header2);
            Assert.IsFalse(header2.Users.Single(q => q.UserId.Equals(recipient)).HasUnreadMessages, "Header shouldn't have any unread messages for this user.");
        }

        [TestMethod]
        public async Task UseCase2MakeSureHeadersAreShownToHeaderUsersOnlyTest()
        {
            // only users 1-3 are in this header and recipients of this message
            var header = await CreatePrivateMessageHeaderAsync();
            await CreatePrivateMessageAsync(header, User1);

            var headers = await ForumServer.Instance.Messages.GetHeadersAsync(User4, 25, 0, false);
            Assert.IsNotNull(headers);
            Assert.IsFalse(headers.Any(q => q.Id.Equals(header.Id)));
        }

        [TestMethod]
        public async Task UseCase3MakeSureNewlyCreatedHeaderAndMessageAreReadTest()
        {
            var header = await CreatePrivateMessageHeaderAsync();
            await CreatePrivateMessageAsync(header, User1);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbHeaderUser = await db.PrivateMessageHeaderUsers.SingleOrDefaultAsync(q => q.PrivateMessageHeaderId.Equals(header.Id) && q.UserId.Equals(User1));
                Assert.IsNotNull(dbHeaderUser);
                Assert.IsFalse(dbHeaderUser.HasUnreadMessages);
            }
        }

        /// <summary>
        /// This test needs to make sure that when you add a user after a few messages that
        /// their HasUnreadMessages flag only signals for messages after they've joined the header.
        /// </summary>
        [TestMethod]
        public async Task UseCase4AddUserLateCheckUnreadStatusTest()
        {
            var header = await CreatePrivateMessageHeaderAsync();
            await CreatePrivateMessageAsync(header, User1);
            await CreatePrivateMessageAsync(header, User2);
            await CreatePrivateMessageAsync(header, User1);

            // add a new user to the header who hasn't read the previous messages (and can't)
            await ForumServer.Instance.Messages.AddUserToHeaderAsync(header, User4);

            // check the user's state before we read any messages
            var header2 = await ForumServer.Instance.Messages.GetHeaderAsync(header.Id);
            Assert.IsNotNull(header2);
            var user4Header2User = header2.Users.SingleOrDefault(q => q.UserId.Equals(User4));
            Assert.IsNotNull(user4Header2User);
            Assert.IsFalse(user4Header2User.HasUnreadMessages);

            // create some new messages for the new user to read
            var message3 = await CreatePrivateMessageAsync(header, User3);
            var message4 = await CreatePrivateMessageAsync(header, User2);

            // check we have read messages for our newly added user
            var header3 = await ForumServer.Instance.Messages.GetHeaderAsync(header.Id);
            var user4Header3User = header3.Users.SingleOrDefault(q => q.UserId.Equals(User4));
            Assert.IsNotNull(user4Header3User);
            Assert.IsTrue(user4Header3User.HasUnreadMessages);

            // read one of our new messages
            await ForumServer.Instance.Messages.MarkMessageAsReadAsync(message3.Id, User4);

            // check that the HasUnreadMessages hasn't changed as we've only read on message
            var header4 = await ForumServer.Instance.Messages.GetHeaderAsync(header.Id);
            var user4Header4User = header4.Users.SingleOrDefault(q => q.UserId.Equals(User4));
            Assert.IsNotNull(user4Header4User);
            Assert.IsTrue(user4Header4User.HasUnreadMessages);

            // read the last of our new messages
            await ForumServer.Instance.Messages.MarkMessageAsReadAsync(message4.Id, User4);

            // check that the HasUnreadMessages hasn't changed as we've only read on message
            var header5 = await ForumServer.Instance.Messages.GetHeaderAsync(header.Id);
            var user4Header5User = header5.Users.SingleOrDefault(q => q.UserId.Equals(User4));
            Assert.IsNotNull(user4Header5User);
            Assert.IsFalse(user4Header5User.HasUnreadMessages);
        }

        #region private methods
        private static async Task<PrivateMessageHeader> CreatePrivateMessageHeaderAsync()
        {
            var header = new PrivateMessageHeader();
            header.Users.Add(new PrivateMessageHeaderUser(User1));
            header.Users.Add(new PrivateMessageHeaderUser(User2));
            header.Users.Add(new PrivateMessageHeaderUser(User3));

            await ForumServer.Instance.Messages.CreateHeaderAsync(header);

            // vaidate the domain model...
            Assert.IsNotNull(header, "header is null");
            Assert.IsTrue(header.Id > 0, "header id is not set");
            Assert.IsTrue(header.Users.Count == 3, "header users count is not 3");
            Assert.IsFalse(header.LastMessageCreated.HasValue, "header has a LastMessageCreated");

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbHeader = await db.PrivateMessageHeaders.Include(q => q.Users).SingleOrDefaultAsync(q => q.Id.Equals(header.Id));
                Assert.IsNotNull(dbHeader, "db header is null");
                Assert.IsNotNull(dbHeader.Users, "db header users is null");
                Assert.IsTrue(dbHeader.Users.Count == 3, "db header users count is not 3");
            }

            return header;
        }

        private static async Task<PrivateMessage> CreatePrivateMessageAsync(PrivateMessageHeader header = null, string user = null)
        {
            if (header == null)
                header = await CreatePrivateMessageHeaderAsync();

            if (user == null)
                user = User1;

            var message = new PrivateMessage
            {
                PrivateMessageHeaderId = header.Id,
                UserId = user,
                Content = "This is a test message."
            };

            await ForumServer.Instance.Messages.CreateMessageAsync(message);

            // vaidate the domain model...
            Assert.IsNotNull(message, "message is null");
            Assert.IsTrue(message.Id > 0, "message id is not set");

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbMessage = await db.PrivateMessages.Include(q => q.ReadBy).SingleOrDefaultAsync(q => q.Id.Equals(message.Id));
                Assert.IsNotNull(dbMessage, "db message is null");
                Assert.AreEqual(dbMessage.PrivateMessageHeaderId, header.Id, $"db header ids not the same: dbMessage.PrivateMessageHeaderId: {dbMessage.PrivateMessageHeaderId} - header.Id: {header.Id}");
                Assert.AreEqual(dbMessage.UserId, user, "db message user id not the same as test user id");
                Assert.AreEqual(dbMessage.Content, message.Content);
                Assert.AreEqual(dbMessage.Type, PrivateMessageType.Message, "db message type is not of message");
                Assert.IsNotNull(dbMessage.ReadBy, "db message readby is null");
                Assert.AreEqual(dbMessage.ReadBy.Count, 0, "db message readby has elements");

                // the following tests are depdendent upon async events having been completed, which might not hve yet done so
                // before the CreateMessageAsync() method returned, so pause for a bit to wait for those events to complete.
                Thread.Sleep(TimeSpan.FromSeconds(1));

                // make sure the header lastMessageSent column has been updated
                var dbHeader = await db.PrivateMessageHeaders.SingleOrDefaultAsync(q => q.Id.Equals(header.Id));
                Assert.IsNotNull(dbHeader, "db header is null");
                Assert.IsTrue(dbHeader.LastMessageCreated.HasValue, "db header doesn't have a LastMessageCreated");
                Assert.AreEqual(dbHeader.LastMessageCreated.Value, dbMessage.Created, "db header LastmessageCreated is not the same as the dbMessage Created");
            }

            return message;
        }

        private static async Task<PrivateMessageHeader> MarkMessageAsReadAsync(string user = null, PrivateMessage message = null)
        {
            if (user == null)
                user = User2;

            var header = await CreatePrivateMessageHeaderAsync();

            if (message == null)
                message = await CreatePrivateMessageAsync(header);

            await ForumServer.Instance.Messages.MarkMessageAsReadAsync(message.Id, user);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                var dbReadBy = await db.PrivateMessageReadBys.SingleOrDefaultAsync(q =>
                q.UserId.Equals(User2) &&
                q.PrivateMessageId.Equals(message.Id));

                Assert.IsNotNull(dbReadBy);
                Assert.AreNotEqual(dbReadBy.When, DateTime.MaxValue);
            }

            return header;
        }

        private static async Task MarkAllMessagesAsReadAsync()
        {
            const string user = User2;
            var header = await CreatePrivateMessageHeaderAsync();
            var messages = new List<PrivateMessage>
            {
                await CreatePrivateMessageAsync(header)//,
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header),
                //await CreatePrivateMessageAsync(header)
            };

            await ForumServer.Instance.Messages.MarkAllMessagesInHeaderAsReadAsync(header.Id, user);

            // vaidate that the database state matches our expectation...
            using (var db = new ForumContext())
            {
                foreach (var message in messages)
                {
                    var dbReadBy = await db.PrivateMessageReadBys.SingleOrDefaultAsync(q => q.UserId.Equals(User2) && q.PrivateMessageId.Equals(message.Id));
                    Assert.IsNotNull(dbReadBy);
                    Assert.AreNotEqual(dbReadBy.When, DateTime.MaxValue);
                }

                var dbHeaderUser =
                    await
                        db.PrivateMessageHeaderUsers.SingleOrDefaultAsync(
                            q => q.UserId.Equals(user) && q.PrivateMessageHeaderId.Equals(header.Id));

                Assert.IsNotNull(dbHeaderUser);
                Assert.AreEqual(false, dbHeaderUser.HasUnreadMessages);
            }
        }

        private static async Task UserHasUnreadMessages(PrivateMessageHeader header = null, string user = null)
        {
            if (header == null)
                header = await MarkMessageAsReadAsync(user);

            if (user == null)
                user = User1;

            using (var db = new ForumContext())
            {
                var dbHeaderUser = await db.PrivateMessageHeaderUsers.SingleOrDefaultAsync(q => q.PrivateMessageHeaderId.Equals(header.Id) && q.UserId.Equals(user));
                Assert.IsNotNull(dbHeaderUser);
                Assert.IsFalse(dbHeaderUser.HasUnreadMessages);
            }
        }
        #endregion
    }
}