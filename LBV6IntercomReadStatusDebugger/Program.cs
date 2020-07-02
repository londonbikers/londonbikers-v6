using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using LBV6ForumApp;
using LBV6Library.Models;

namespace LBV6IntercomReadStatusDebugger
{
    internal class Program
    {
        private static void Main()
        {
            var headerUsersToFix = new List<PrivateMessageHeaderUser>();
            using (var db = new ForumContext())
            {
                var dbHeaderUsers = db.PrivateMessageHeaderUsers.Where(q => q.HasUnreadMessages).ToList();
                foreach (var dbHeaderUser in dbHeaderUsers)
                {
                    Console.WriteLine($"Header: {dbHeaderUser.PrivateMessageHeaderId}, User: {dbHeaderUser.UserId}");

                    // get all messages and their ready-bys in this header for messages that weren't authored by this user
                    var dbUserReceivedMessages =
                        db.PrivateMessages.Where(
                            q => q.PrivateMessageHeaderId.Equals(dbHeaderUser.PrivateMessageHeaderId) &&
                                 q.Type == PrivateMessageType.Message &&
                                 !q.UserId.Equals(dbHeaderUser.UserId) &&
                                 q.Created >= dbHeaderUser.Added).Include(q => q.ReadBy).ToList();

                    Console.WriteLine($"\tMessages: {dbUserReceivedMessages.Count}");

                    // do any messages not have a read-by for this user? (meaning PrivateMessageHeaderUser.HasUnreadMessages should be true)
                    var readyBysForUser = 0;
                    foreach (var dbMessage in dbUserReceivedMessages.Where(dbMessage => dbMessage.ReadBy.Count(q => q.UserId.Equals(dbHeaderUser.UserId)) > 0))
                    {
                        Console.WriteLine($"\t\tRead-By[ User: {dbHeaderUser.UserId}, Message: {dbMessage.Id} ]");
                        readyBysForUser++;
                    }

                    if (readyBysForUser != dbUserReceivedMessages.Count)
                        continue;

                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine($"\tUser {dbHeaderUser.UserId} has read all messages in this header ({dbHeaderUser.PrivateMessageHeaderId})!");
                    Console.ResetColor();

                    // mark this header user as needing to be fixed
                    headerUsersToFix.Add(dbHeaderUser);
                }

                Console.WriteLine(@"-------------------------------------");
                Console.WriteLine($"There were {headerUsersToFix.Count} header users out of sync.");

                #region fix header users
                if (headerUsersToFix.Count > 0)
                {
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.WriteLine(@"Fix these header users? Y/N");
                    Console.ResetColor();
                    var fix = Console.ReadKey(true).KeyChar.ToString().ToLowerInvariant();
                    if (fix == "y")
                    {
                        foreach (var dbHeaderUserToFix in headerUsersToFix)
                            dbHeaderUserToFix.HasUnreadMessages = false;

                        db.SaveChanges();
                        Console.WriteLine(@"Fixed header users.");
                    }
                    else
                    {
                        Console.WriteLine(@"Not fixing header users.");
                    }
                } 
                #endregion

                Console.WriteLine(@"-------------------------------------");
            }

            
            Console.WriteLine(@"Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}