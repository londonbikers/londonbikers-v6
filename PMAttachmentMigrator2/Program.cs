using LBV6ForumApp;
using LBV6Library;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Threading;

namespace PMAttachmentMigrator2
{
    internal class Program
    {
        private static void Main()
        {
            var uploads = 0;
            Console.WriteLine($@"attachmentsPath: {ConfigurationManager.AppSettings["attachmentsPath"]}");
            Console.WriteLine($@"connectionString: {ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString}");

            // enumerate attachments table
            //  upload to cloud storage
            //  write id back to attachments table

            var missingImageAttachmentIds = new List<string>();
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand())
                {
                    command.Connection = connection;
                    command.CommandText = "SELECT PMA.[Id], PMA.[Filename] FROM [PrivateMessageAttachments] PMA WHERE PMA.[FilestoreId] IS NULL";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // get the file
                            var filename = reader["Filename"].ToString();
                            var filePath = Path.Combine(ConfigurationManager.AppSettings["attachmentsPath"], filename);
                            if (!File.Exists(filePath))
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine($@"File '{filename}' doesn't exist! Skipping");
                                Console.ResetColor();
                                missingImageAttachmentIds.Add(reader["Id"].ToString());
                                continue;
                            }

                            #region get image details
                            Size size;
                            using (var image = Image.FromFile(filePath))
                                size = Utilities.GetAutoRotatedImageDimensions(image);

                            //long contentLength;
                            //using (var file = File.OpenRead(filePath))
                            //    contentLength = file.Length;

                            var fileId = Guid.NewGuid().ToString();
                            #endregion

                            #region update database record
                            using (var updateConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                            {
                                updateConnection.Open();
                                using (var updateCommand = new SqlCommand())
                                {
                                    updateCommand.Connection = updateConnection;
                                    updateCommand.CommandText = $"UPDATE PrivateMessageAttachments SET Width = {size.Width}, Height = {size.Height}, FilestoreId = '{fileId}' WHERE Id = {reader["Id"]}";
                                    updateCommand.ExecuteNonQuery();
                                }
                            }
                            #endregion

                            #region upload file to object store
                            try
                            {
                                var files = ForumServer.Instance.Photos.GetFilesProvider();
                                using (var stream = File.OpenRead(filePath))
                                {
                                    stream.Seek(0, SeekOrigin.Begin);
                                    files.CreateObject(ForumServer.Instance.Photos.PrivateMessagePhotosContainer, stream, fileId);
                                    Console.WriteLine(@"Uploaded photo: " + filename);
                                }

                                uploads++;
                            }
                            catch (Exception ex)
                            {
                                using (var undoConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                                {
                                    undoConnection.Open();
                                    using (var undoCommand = new SqlCommand())
                                    {
                                        undoCommand.Connection = undoConnection;
                                        undoCommand.CommandText = $"UPDATE PrivateMessageAttachments SET Width = NULL, Height = NULL, FilestoreId = NULL WHERE Id = {reader["Id"]}";
                                        undoCommand.ExecuteNonQuery();
                                    }
                                }

                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine($@"Couldn't upload attachment! Undid changes from db. File: {filename}. Exception: " + ex.Message);
                                Console.ResetColor();

                                if (!ex.Message.Contains("429"))
                                {
                                    Console.WriteLine(@"Press any key to exit.");
                                    Console.ReadKey(true);
                                    return;
                                }

                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Console.WriteLine(@"Rate-limited by OVH! Pausing for 30secs.");
                                Console.ResetColor();
                                Thread.Sleep(TimeSpan.FromSeconds(30));
                            }
                            #endregion
                        }
                    }
                }
            }

            using (var deleteConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            {
                deleteConnection.Open();
                using (var deleteCommand = new SqlCommand())
                {
                    deleteCommand.Connection = deleteConnection;
                    foreach (var id in missingImageAttachmentIds)
                    {
                        deleteCommand.CommandText = $@"DELETE FROM [PrivateMessageAttachments] WHERE Id = {id}";
                        deleteCommand.ExecuteNonQuery();
                    }
                }
            }

            Console.WriteLine($@"Uploaded {uploads} PM attachments.");
            Console.WriteLine($@"Deleted {missingImageAttachmentIds.Count} PM attachments with missing attachments.");
            
            Console.WriteLine(@"Press any key to exit.");
            Console.ReadKey(true);
        }
    }
}