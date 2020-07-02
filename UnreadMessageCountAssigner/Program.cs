using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace UnreadMessageCountAssigner
{
    internal class Program
    {
        private static void Main()
        {
            var ids = new List<string>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                Console.WriteLine("Getting user ids...");

                connection.Open();
                using (var idCommand = new SqlCommand("SELECT Id FROM aspnetusers", connection))
                using (var idReader = idCommand.ExecuteReader())
                    while (idReader.Read())
                        ids.Add((string)idReader[0]);

                foreach (var id in ids)
                {
                    using (var getCountCommand = new SqlCommand("GetUnreadMessageCountForUser", connection))
                    {
                        getCountCommand.CommandType = CommandType.StoredProcedure;
                        getCountCommand.Parameters.AddWithValue("@UserId", id);

                        var count = 0;
                        var result = getCountCommand.ExecuteScalar();
                        if (result != DBNull.Value)
                            count = Convert.ToInt32(result);

                        Console.WriteLine($"User {id} has {count} unread messages");
                        if (count <= 0) continue;

                        using (var updateUserCommand = new SqlCommand($"UPDATE aspnetusers SET UnreadMessagesCount = {count} WHERE Id = '{id}'", connection))
                        {
                            Console.WriteLine($"Updating user {id} count!");
                            updateUserCommand.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
    }
}