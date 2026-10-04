namespace Asset.Application.Common.Caching
{
    // Which TTL group a cached query belongs to.
    // The real minutes live in appsettings.json.
    public enum CacheDuration
    {
        Lookup,     // reference data: long TTL
        Volatile    // live data: short TTL
    }
}