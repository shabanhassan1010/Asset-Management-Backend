using Asset.Application.Common.Caching;
using MediatR;

namespace Asset.Application.Behaviors
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull ,  ICachedQuery
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
            // 1. Cache hit -> return it, the handler never runs and the database is never touched.
            var cached = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
            if (cached is not null)
                return cached;

            // 2. Cache miss -> run the handler as normal.
            var response = await next();

            // 3. Store the result for the next request.
            if (response is not null && request.Duration > TimeSpan.Zero)
                await _cache.SetAsync(request.CacheKey, response, request.Duration, cancellationToken);

            return response;
        }
    }
}