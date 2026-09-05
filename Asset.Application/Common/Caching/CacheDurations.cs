namespace Asset.Application.Common.Caching
{
    public static class CacheDurations
    {
        // Lookup data: changes rarely, safe to hold for a while.
        public static readonly TimeSpan Lookup = TimeSpan.FromMinutes(20);

        // Anything that reflects live operational state should be shorter.
        public static readonly TimeSpan Volatile = TimeSpan.FromMinutes(2);
    }
}