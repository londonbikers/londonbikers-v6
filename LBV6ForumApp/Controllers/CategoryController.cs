using LBV6Library;
using LBV6Library.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace LBV6ForumApp.Controllers
{
    public class CategoryController
    {
        #region members
        private List<Category> _categories;
        #endregion

        #region accessors
        public List<Category> Categories
        {
            get
            {
                if (_categories == null)
                    GetCategories();

                return _categories;
            }
            internal set { _categories = value; }
        }
        #endregion

        #region constructors
        internal CategoryController()
        {
        }
        #endregion

        #region public methods
        public async Task UpdateCategoryAsync(Category category)
        {
            if (category == null)
                throw new ArgumentNullException(nameof(category));

            using (var db = new ForumContext())
            {
                if (category.Id < 1)
                {
                    // make sure the category is positioned as the bottom one
                    category.Order = Categories.Count;
                    db.Categories.Add(category);
                }
                else
                {
                    var dbCategory = await db.Categories.SingleOrDefaultAsync(q => q.Id.Equals(category.Id));
                    if (dbCategory == null)
                        throw new Exception("Category not found. Either it's been deleted or it hasn't yet been persisted.");

                    if (!dbCategory.Name.Equals(category.Name))
                        dbCategory.Name = category.Name;

                    if (!string.IsNullOrEmpty(dbCategory.Description) && string.IsNullOrEmpty(category.Description))
                        dbCategory.Description = null; // if we have a database value and we don't have one in the model, remove the value
                    else if ((string.IsNullOrEmpty(dbCategory.Description) && !string.IsNullOrEmpty(category.Description)) || (!string.IsNullOrEmpty(dbCategory.Description) && !string.IsNullOrEmpty(category.Description) && !dbCategory.Description.Equals(category.Description)))
                        dbCategory.Description = category.Description; // if we don't have a database value and do have a model value, or if the database value is different to the model value, assign the value

                    if (!dbCategory.Order.Equals(category.Order))
                        dbCategory.Order = category.Order;

                    if (!dbCategory.IsGalleryCategory.Equals(category.IsGalleryCategory))
                        dbCategory.IsGalleryCategory = category.IsGalleryCategory;
                }

                await db.SaveChangesAsync();

                #region cached categories list maintenance
                Categories = null;
                #endregion
            }
        }

        public async Task DeleteCategoryAsync(int categoryId)
        {
            if (categoryId < 1)
                throw new ArgumentException("categoryId is not valid.");

            using (var db = new ForumContext())
            {
                var category = await db.Categories.SingleOrDefaultAsync(q => q.Id.Equals(categoryId));
                if (category == null)
                    return;

                if (category.Forums.Count > 0)
                    throw new InvalidOperationException("The category has forums so cannot be deleted. Move the forums to another category first.");

                db.Categories.Remove(category);
                await db.SaveChangesAsync();

                // maintain the cache
                Categories.RemoveAll(q => q.Id.Equals(categoryId));
            }
        }
        #endregion

        #region private methods
        private void GetCategories()
        {
            // can't make this async as the calling code is an accessor
            using (var db = new ForumContext())
            {
                _categories = db.Categories
                    .Include(q => q.Forums)
                    .Include("Forums.AccessRoles")
                    .Include("Forums.PostRoles")
                    .OrderBy(q => q.Order).ToList();
            }

            var indexSize = Convert.ToInt32(ConfigurationManager.AppSettings["LB.RecentTopicsIndexSize"]);
            foreach (var category in _categories)
            {
                // order the forums (not sure how this can be done via the initial EF query)
                category.Forums = category.Forums.OrderBy(q => q.Order).ToList();

                // get a list of recent topics for the forum to use as a recent index cache
                foreach (var forum in category.Forums)
                    forum.RecentTopicsIndex = AsyncHelpers.RunSync(() => ForumServer.Instance.Posts.GetTopicIdsForForumAsync(forum.Id, indexSize, 0));
            }
        }
        #endregion
    }
}