namespace Asset.Application.Common.Caching
{
    public interface ICacheService
    {
        // T? because a miss is a normal result, not an error.
        Task<T?> GetAsync<T>(string key, CancellationToken ct);
        Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct);

        // Used by command handlers to invalidate a key after a write.
        Task RemoveAsync(string key);
        Task RemoveAsync(IEnumerable<string> keys);


        // Versioned keys (used for assets). Returns the current version of a group, or "0" if it has none yet.
        Task<string> GetVersionAsync(string versionKey, CancellationToken ct);          

        // Gives the group a new version, so every key built with the old one is never read again.
        // No CancellationToken: like RemoveAsync, it runs after SaveChanges and must finish.
        Task BumpVersionAsync(string versionKey);

    }
}