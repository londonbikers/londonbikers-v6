using LBV6Library;
using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    public class ForumController
    {
        #region constructors
        internal ForumController()
        {
        }
        #endregion

        #region crud methods
        public Forum GetForum(long id)
        {
            return ForumServer.Instance.Categories.Categories.SelectMany(c => c.Forums.Where(f => f.Id.Equals(id))).FirstOrDefault();
        }

        public async Task UpdateForumAsync(Forum forum)
        {
            try
            {
                if (forum == null)
                    throw new ArgumentNullException(nameof(forum));

                using (var db = new ForumContext())
                {
                    if (forum.Id < 1)
                    {
                        db.Forums.Add(forum);
                    }
                    else
                    {
                        var dbForum = await db.Forums.SingleOrDefaultAsync(q => q.Id.Equals(forum.Id));
                        if (dbForum == null)
                            throw new Exception("Forum not found. Either it's been deleted or it hasn't yet been persisted.");

                        if (!dbForum.Name.Equals(forum.Name))
                            dbForum.Name = forum.Name;

                        if (!string.IsNullOrEmpty(dbForum.Description) && string.IsNullOrEmpty(forum.Description))
                            dbForum.Description = null; // if we have a database value and we don't have one in the model, remove the value
                        else if ((string.IsNullOrEmpty(dbForum.Description) && !string.IsNullOrEmpty(forum.Description)) || (!string.IsNullOrEmpty(dbForum.Description) && !string.IsNullOrEmpty(forum.Description) && !dbForum.Description.Equals(forum.Description)))
                            dbForum.Description = forum.Description; // if we don't have a database value and do have a model value, or if the database value is different to the model value, assign the value

                        if (!dbForum.Order.Equals(forum.Order))
                            dbForum.Order = forum.Order;

                        if (!dbForum.LastUpdated.Equals(forum.LastUpdated))
                            dbForum.LastUpdated = forum.LastUpdated;

                        if (!dbForum.PostCount.Equals(forum.PostCount))
                            dbForum.PostCount = forum.PostCount;

                        if (!dbForum.CategoryId.Equals(forum.CategoryId))
                            dbForum.CategoryId = forum.CategoryId;

                        if (!dbForum.ProtectTopicPhotos.Equals(forum.ProtectTopicPhotos))
                            dbForum.ProtectTopicPhotos = forum.ProtectTopicPhotos;

                        // remove any forum access roles no longer present
                        var accessRolesToRemove = new List<ForumAccessRole>();
                        foreach (var dbRole in db.ForumAccessRoles.Where(q => q.ForumId.Equals(forum.Id)))
                            if (!forum.AccessRoles.Any(q => q.Id.Equals(dbRole.Id)))
                                accessRolesToRemove.Add(dbRole);
                        db.ForumAccessRoles.RemoveRange(accessRolesToRemove);

                        // add any new forum access roles
                        foreach (var forumAccessRole in forum.AccessRoles.Where(q => q.Id == 0))
                            db.ForumAccessRoles.Add(forumAccessRole);

                        // remove any forum post roles no longer present
                        var postRolesToRemove = new List<ForumPostRole>();
                        foreach (var dbForumPostRole in db.ForumPostRoles.Where(q => q.ForumId.Equals(forum.Id)))
                            if (!forum.PostRoles.Any(q => q.Id.Equals(dbForumPostRole.Id)))
                                postRolesToRemove.Add(dbForumPostRole);
                        db.ForumPostRoles.RemoveRange(postRolesToRemove);

                        // add any new forum post roles
                        foreach (var forumPostRole in forum.PostRoles.Where(q => q.Id == 0))
                            db.ForumPostRoles.Add(forumPostRole);
                    }

                    await db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                throw;
            }
        }

        public async Task DeleteForumAsync(long id)
        {
            try
            {
                if (id < 1)
                    throw new ArgumentException("id is not valid.");

                using (var db = new ForumContext())
                {
                    var forum = await db.Forums.SingleOrDefaultAsync(q => q.Id.Equals(id));
                    if (forum == null)
                        return;

                    if (forum.PostCount > 0)
                        throw new InvalidOperationException("The forum has posts so cannot be deleted. Move the posts to another forum first.");

                    db.Forums.Remove(forum);
                    await db.SaveChangesAsync();

                    #region cached categories list maintenance
                    // forums are stored in categories, so just expire the category cache
                    ForumServer.Instance.Categories.Categories = null;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Logging.LogError(GetType().FullName, ex);
                throw;
            }
        }
        #endregion

        #region internal methods
        /// <summary>
        /// Sometimes it is necessary to update forum stats when posts
        /// are moved or removed. This will perform all necessary statistics updates.
        /// </summary>
        internal async Task UpdateForumStatsAsync(long id)
        {
            var forum = GetForum(id);
            using (var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
            using (var command = new SqlCommand("GetForumStats", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@ForumId", id);
                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (reader.Read())
                    {
                        // now update our cached model
                        lock (forum)
                        {
                            forum.LastUpdated = (DateTime)reader["LastUpdated"];
                            forum.PostCount = (int)reader["PostCount"];
                        }
                    }
                }
            }

            // persist stats back to database
            using (var db = new ForumContext())
            {
                var dbForum = await db.Forums.SingleAsync(q => q.Id.Equals(id));
                dbForum.PostCount = forum.PostCount;
                dbForum.LastUpdated = forum.LastUpdated;
                await db.SaveChangesAsync();
            }
        }
        #endregion
    }
}