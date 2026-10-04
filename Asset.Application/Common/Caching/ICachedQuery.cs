namespace Asset.Application.Common.Caching
{
    public interface ICachedQuery
    {
        string CacheKey { get; }

        // The group, not the minutes: the query can't read appsettings.json,
        // so CachingBehavior turns the group into minutes using CacheSettings.
        CacheDuration Duration { get; }
    }
}