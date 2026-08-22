using Asset.Application.Common.Caching;
using MediatR;

namespace Asset.Application.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull
    {
        #region Fields
        private readonly ICacheService _cache;
        #endregion

        #region Constructor
        public CachingBehavior(ICacheService cache)
        {
            _cache = cache;
        }
        #endregion

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,CancellationToken cancellationToken)
        {
            // Not a cached query (any command, or a query we chose not to cache) -> straight to the handler.
            if (request is not ICachedQuery cachedQuery)
                return await next();

            // 1. Cache hit -> return it, the handler never runs and the database is never touched.
            var cached = await _cache.GetAsync<TResponse>(cachedQuery.CacheKey, cancellationToken);
            if (cached is not null)
                return cached;

            // 2. Cache miss -> run the handler as normal.
            var response = await next();

            // 3. Store the result for the next request.
            if (response is not null)
                await _cache.SetAsync(cachedQuery.CacheKey, response, cachedQuery.Duration, cancellationToken);

            return response;
        }
    }
}
