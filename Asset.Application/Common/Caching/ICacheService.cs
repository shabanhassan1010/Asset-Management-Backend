namespace Asset.Application.Common.Caching
{
    public interface ICacheService
    {
        // T? because a miss is a normal result, not an error.
        Task<T?> GetAsync<T>(string key, CancellationToken ct);

        Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct);

        // Used by command handlers to invalidate a key after a write.
        Task RemoveAsync(string key, CancellationToken ct);
    }
}
