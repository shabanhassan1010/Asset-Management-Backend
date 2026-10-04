using Asset.Application.Common.Caching;
using Asset.Application.Interfaces.Comman;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace Asset.Application.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull ,  ICachedQuery
    {
        #region Fields
        private readonly ICacheService _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private readonly CacheSettings _settings; 
        private readonly ICurrentUserService _currentUserService;
        #endregion

        #region Constructor
        public CachingBehavior(ICacheService cache, 
                               ILogger<CachingBehavior<TRequest, TResponse>> logger, 
                               IOptions<CacheSettings> settings, 
                               ICurrentUserService currentUserService)
        {
            _cache = cache;
            _logger = logger;
            _settings = settings.Value;
            _currentUserService = currentUserService;
        }
        #endregion

        #region Methods
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
        {
            // Build the real key once (CacheKey + role + version). Use it everywhere below.
            var cacheKey = await BuildKey(request, cancellationToken);
            // Cache hit -> return it,Try to get the response from cache, the handler never runs and the database is never touched.
            var cached = await _cache.GetAsync<TResponse>(cacheKey, cancellationToken);
            // Cache HIT
            if (cached is not null)
            {
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                // Handler will NOT run, Database will NOT be accessed
                return cached;
            }

            _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);    

            // 2. Cache miss -> run the handler as normal.
            var response = await next();

            // 3. Store the result for the next request.
            // 3. Store the result, for as long as appsettings.json says.
            var duration = _settings.GetDuration(request.Duration);
            if (response is not null && duration > TimeSpan.Zero)
                await _cache.SetAsync(cacheKey, response, duration, cancellationToken);

            return response;
        }
        #endregion

        #region Private Methods
        // Builds the real key. If the response depends on the role, the role becomes part of the key, so an Admin response is never served to a User.
        private async Task<string> BuildKey(TRequest request, CancellationToken ct)                                  
        {
            var key = request.CacheKey;

            if (request is IVaryByRole)      
                key += _currentUserService.IsAdmin ? ":role:admin" : ":role:user";
            
            if(request is IVersionedCache versioned)
            {
                var version = await _cache.GetVersionAsync(versioned.VersionKey, ct);
                key += $":v{version}";
            }
            return key;
        }
        #endregion
    }
}


#region Flow
//                         Request
//                            │
//                            ▼
//                         Get from Redis
//                            │
//                            ├── HIT ──────► Return cached response
//                            │                ❌ Handler doesn't run
//                            │                ❌ Database isn't queried
//                            │
//                             └── MISS
//                            │
//                            ▼
//                     Handler executes
//                            │
//                            ▼
//                         Database
//                            │
//                            ▼
//                         Response
//                            │
//                            ▼
//                      Save in Redis
//                            │
//                            ▼
//                     Return response
#endregion