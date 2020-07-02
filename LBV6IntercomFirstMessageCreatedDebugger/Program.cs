using System;
using System.Collections.Generic;
using System.Linq;
using LBV6ForumApp;

namespace LBV6IntercomFirstMessageCreatedDebugger
{
    /// <summary>
    /// WARNING: This is only of use when users have not been added to a header after creation.
    /// </summary>
    internal class Program
    {
        internal struct MessageCreatedCorrection
        {
            internal DateTime NewDateCreated { get; set; }
            internal long MessageId { get; set; }
        }

        private static void Main()
        {
            // task:
            // go through each header and make note of the last user added date
            // go through each message in the header and make sure the created dates are greater or equal to the last user added date

            var corrections = new List<MessageCreatedCorrection>();
            using (var db = new ForumContext())
            {
                Console.WriteLine(@"Getting headers...");
                foreach (var dbHeader in db.PrivateMessageHeaders.Include("Users").Where(q => q.Users.Count > 0))
                {
                    var lastUserAddedDate = dbHeader.Users.Max(q => q.Added);
                    using (var db2 = new ForumContext())
                    {
                        Console.WriteLine(@"Getting invisible messages for header: " + dbHeader.Id);
                        foreach (var dbMessage in db2.PrivateMessages.Where(q => q.PrivateMessageHeaderId.Equals(dbHeader.Id) && q.Created < lastUserAddedDate).OrderBy(q => q.Created))
                        {
                            // we need to move the date on a bit so that when there's multiple messages to correct, we preserve their order
                            lastUserAddedDate = lastUserAddedDate.AddMilliseconds(10);

                            Console.WriteLine($"Invisible message detected! Header: {dbHeader.Id}, Message: {dbMessage.Id}, Created: {dbMessage.Created.ToString("O")}, New Created: {lastUserAddedDate.ToString("O")}");
                            corrections.Add(new MessageCreatedCorrection
                            {
                                MessageId = dbMessage.Id,
                                NewDateCreated = lastUserAddedDate
                            });
                        }
                    }
                }
            }

            Console.WriteLine(@"-------------------------------------------------");
            Console.WriteLine($"There are {corrections.Count} corrections needed.");

            if (corrections.Count > 0)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.WriteLine(@"Fix these message dates? Y/N");
                Console.ResetColor();
                var fix = Console.ReadKey(true).KeyChar.ToString().ToLowerInvariant();
                if (fix == "y")
                {
                    using (var db = new ForumContext())
                    {
                        foreach (var correction in corrections)
                        {
                            var dbMessage = db.PrivateMessages.Single(q => q.Id.Equals(correction.MessageId));
                            dbMessage.Created = correction.NewDateCreated;
                        }

                        db.SaveChanges();
                    }
                }
                else
                {
                    Console.WriteLine(@"Not fixing message dates.");
                }
            }

            Console.WriteLine(@"-------------------------------------------------");
            Console.WriteLine(@"Press any key to exit.");
            Console.ReadKey(true);
        }
    }
}