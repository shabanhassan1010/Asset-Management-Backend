namespace Asset.Application.Common.Caching
{
    public interface ICachedQuery
    {
        string CacheKey { get; }

        // Per-query, because a lookup list can live for an hour while a
        // dashboard summary should not.
        TimeSpan Duration { get; }
    }
}
