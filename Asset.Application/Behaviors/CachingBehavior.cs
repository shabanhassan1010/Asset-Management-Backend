using Asset.Application.Common.Caching;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Asset.Application.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull ,  ICachedQuery
    {
        #region Fields
        private readonly ICacheService _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        #endregion

        #region Constructor
        public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _cache = cache;
            _logger = logger;
        }
        #endregion

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
        {
            // 1. Cache hit -> return it,Try to get the response from cache, the handler never runs and the database is never touched.
            var cached = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
            // 2. Cache HIT
            if (cached is not null)
            {
                _logger.LogInformation("Cache hit for {CacheKey}. Returning cached response.", request.CacheKey);
                // Handler will NOT run, Database will NOT be accessed
                return cached;
            }

            _logger.LogInformation("Cache miss for {CacheKey}", request.CacheKey);    

            // 2. Cache miss -> run the handler as normal.
            var response = await next();

            // 3. Store the result for the next request.
            if (response is not null && request.Duration > TimeSpan.Zero)
                await _cache.SetAsync(request.CacheKey, response, request.Duration, cancellationToken);

            return response;
        }
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