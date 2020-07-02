using LBV6Library;
using LBV6Library.Interfaces;
using LBV6Library.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;

namespace LBV6ForumApp.Controllers
{
    internal class CacheController
    {
        #region members
        private readonly bool _cachingEnabled;
        #endregion

        #region constructors
        internal CacheController()
        {
            _cachingEnabled = bool.Parse(ConfigurationManager.AppSettings["LB.EnableCaching"]);
        }
        #endregion

        internal void Add(ICachable entity)
        {
            if (!_cachingEnabled) 
                return;

            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (MemoryCache.Default.Contains(entity.CacheKey))
                return;

            Logging.LogDebug(GetType().FullName, "Adding entity to cache: " + entity.CacheKey);
            MemoryCache.Default.Add(entity.CacheKey, entity, new CacheItemPolicy());
        }

        /// <summary>
        /// Adds any non-domain object to the cache(s), i.e. reference-data lists.
        /// </summary>
        /// <param name="key">The unique identifier by which to retrieve the object in the future.</param>
        /// <param name="entity">The non-domain object to cache.</param>
        /// <param name="removable">If false, under memory pressure, the object will not be removed.</param>
        /// <param name="timeToCacheFor">Items can be cached for a fixed amount of time.</param>
        internal void Add(string key, object entity, bool removable = false, TimeSpan? timeToCacheFor = null)
        {
            if (!_cachingEnabled)
                return;

            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty!");

            if (entity is INotCachable)
                throw new ArgumentException("objectToCache cannot be cached.");

            if (entity is ICachable)
                throw new ArgumentException("objectToCache cannot be cached using this method. Use Add(ICachable entity) instead.");

            if (MemoryCache.Default.Contains(key))
                return;

            var policy = new CacheItemPolicy { Priority = removable ? CacheItemPriority.Default : CacheItemPriority.NotRemovable };
            if (timeToCacheFor.HasValue)
                policy.AbsoluteExpiration = new DateTimeOffset(DateTime.UtcNow + timeToCacheFor.Value);

            Logging.LogDebug(GetType().FullName, "Adding object to cache: " + key);
            MemoryCache.Default.Add(key, entity, policy);
        }

        /// <summary>
        /// Retrieves an item from the cache. Returns null if the item is not cached.
        /// </summary>
        internal object Get(string key)
        {
            if (!_cachingEnabled)
                return null;

            var item = MemoryCache.Default.Get(key);
            if (item != null)
                Logging.LogDebug(GetType().FullName, "Cache HIT for: " + key);
            if (item == null)
                Logging.LogDebug(GetType().FullName, "Cache miss for: " + key);
            return item;
        }

        internal Post GetLegacyPost(int legacyPostId)
        {
            if (!_cachingEnabled)
                return null;

            var item = MemoryCache.Default.SingleOrDefault(q => q.Value.GetType() == typeof(Post) && ((Post) q.Value).LegacyPostId.Equals(legacyPostId));
            if (item.Value != null)
                Logging.LogDebug(GetType().FullName, "Cache HIT for legacy post: " + legacyPostId);

            if (item.Value != null) return (Post) item.Value;
            Logging.LogDebug(GetType().FullName, "Cache miss for legacy post: " + legacyPostId);
            return null;
        }
        
        internal User GetUserByUsername(string username)
        {
            if (!_cachingEnabled)
                return null;
            
            var item = MemoryCache.Default.SingleOrDefault(q => q.Value.GetType() == typeof(User) && ((User)q.Value).UserName.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (item.Value != null)
                Logging.LogDebug(GetType().FullName, "Cache HIT for user: " + username);
                
            if (item.Value != null)
                return (User) item.Value;
              
            Logging.LogDebug(GetType().FullName, "Cache miss for user: " + username);
            return null;
        }

        internal User GetUserByEmail(string email)
        {
            if (!_cachingEnabled)
                return null;

            var item = MemoryCache.Default.SingleOrDefault(q => q.Value.GetType() == typeof(User) && ((User)q.Value).Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (item.Value != null)
                Logging.LogDebug(GetType().FullName, "Cache HIT for user: " + email);

            if (item.Value != null)
                return (User)item.Value;

            Logging.LogDebug(GetType().FullName, "Cache miss for user: " + email);
            return null;
        }

        /// <summary>
        /// Expire an item from the cache
        /// </summary>
        internal void Remove(string key)
        {
            if (!_cachingEnabled)
                return;

            var removedObject = MemoryCache.Default.Remove(key);
            if (removedObject != null)
                Logging.LogDebug(GetType().FullName, "Item removed from cache: " + key);
            if (removedObject == null)
                Logging.LogDebug(GetType().FullName, "No item to remove from cache: " + key);
        }
    }
}