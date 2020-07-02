using System.ComponentModel.DataAnnotations.Schema;

namespace LBV6Library.Interfaces
{
    /// <summary>
    /// Add to all domain models that can be cached. Aids the CacheManager in enforcing caching policy.
    /// </summary>
    public interface ICachable
    {
        [NotMapped]
        string CacheKey { get; }
    }
}