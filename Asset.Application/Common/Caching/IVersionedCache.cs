namespace Asset.Application.Common.Caching
{
    // Add it to a cached query whose keys can't be deleted one by one (e.g. the asset list).
    // CachingBehavior puts the group's current version into the key; a write bumps the
    // version, so every old key is never read again (R5.3).
    public interface IVersionedCache
    {
        string VersionKey { get; }     // e.g. CacheKeys.AssetsVersion
    }
}