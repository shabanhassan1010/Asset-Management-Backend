namespace Asset.Application.Common.Caching
{
    public enum CacheDuration
    {
        Lookup,     // reference data: long TTL
        Volatile    // live data: short TTL
    }
}