namespace Asset.Application.Common.Caching
{
    // Filled from the "Cache" section of appsettings.json (R5.5).
    public class CacheSettings
    {
        public const string SectionName = "Cache";

        // Put in front of every key: "asset:" + "departments:list" = "asset:departments:list".
        public string KeyPrefix { get; set; } = "asset:";

        // TTL in minutes for each group. 0 = don't cache that group.
        public int LookupMinutes { get; set; } = 60;
        public int VolatileMinutes { get; set; } = 2;

        // Turns a group into a real TimeSpan.
        public TimeSpan GetDuration(CacheDuration duration)
        {
            return duration switch
            {
                CacheDuration.Lookup => TimeSpan.FromMinutes(LookupMinutes),
                CacheDuration.Volatile => TimeSpan.FromMinutes(VolatileMinutes),
                _ => TimeSpan.Zero
            };
        }
    }
}