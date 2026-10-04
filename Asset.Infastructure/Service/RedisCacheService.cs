#region
using Asset.Application.Common.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;
#endregion
namespace Asset.Infastructure.Service
{
    public class RedisCacheService : ICacheService
    {
        #region Fields
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        // Created once and reused — building JsonSerializerOptions per call is wasteful.
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
        #endregion

        #region Constructor
        public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        #endregion

        #region Methods
        public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
        {
            try
            {
                var json = await _cache.GetStringAsync(key, ct);

                if (string.IsNullOrEmpty(json))
                    return default;   // miss

                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            catch (OperationCanceledException)
            {           
                throw; // The caller cancelled. This is not a cache failure — do not  swallow it, or the handler would run for a dead request.
            } 
            catch (Exception ex)
            {
                // The cache is an optimisation, not a source of truth.
                // If Redis is down, log it and report a miss so the request
                // falls through to the database instead of failing.
                _logger.LogWarning(ex, "Cache read failed for key {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken ct)
        {
            try
            {
                var json = JsonSerializer.Serialize(value, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    // Absolute, not sliding: a lookup list must refresh on a
                    // predictable schedule even if it is read constantly.
                    AbsoluteExpirationRelativeToNow = duration
                };

                await _cache.SetStringAsync(key, json, options, ct);
            }
            catch (OperationCanceledException)
             {
                throw;
             }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key,CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Worth a warning, not a silent pass: a failed invalidation
                // means clients keep reading stale data until the key expires.
                _logger.LogWarning(ex, "Cache invalidation failed for key {Key}", key);
            }
        }

        public async Task RemoveAsync(IEnumerable<string> keys)
        {
            foreach (var key in keys)
                await RemoveAsync(key);
        }

        public async Task<string> GetVersionAsync(string versionKey, CancellationToken ct)
        {
            try
            {
                var version = await _cache.GetStringAsync(versionKey, ct);

                // No version yet (first run, or Redis was restarted) → start from "0".
                return string.IsNullOrEmpty(version) ? "0" : version;
            }
            catch (OperationCanceledException)
            {
                throw;   // the caller cancelled — same rule as GetAsync
            }
            catch (Exception ex)
            {
                // Redis down: report "0". The cache read right after will fail too,
                // so the request simply goes to the database (R5.6).
                _logger.LogWarning(ex, "Cache version read failed for key {Key}", versionKey);
                return "0";
            }
        }

        public async Task BumpVersionAsync(string versionKey)
        {
            try
            {
                // A new unique value: the current time in ticks (changes every 100 nanoseconds).
                var newVersion = DateTime.UtcNow.Ticks.ToString();

                // new DistributedCacheEntryOptions() = no expiration: the version must not disappear.
                // CancellationToken.None: the database is already updated, so this must finish.
                await _cache.SetStringAsync(versionKey, newVersion, new DistributedCacheEntryOptions(), CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache version bump failed for key {Key}", versionKey);
            }
        }
        #endregion
    }
}