using Asset.Application.Common.Caching;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System.Collections.Generic;

namespace Asset.Application.Behaviors
{
    /// <summary>
    /// Make [MediatR Pipeline Behavior] 
    /// to recive the request and check if the response is already cached in Redis, if yes return the cached response.
    /// If not call the handler and cache the response for future requests.
    /// Where TRequest : notnull, ICachedQuery >>>> this means that the request must be a not null and implement the ICachedQuery interface.
    /// </summary>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>  where TRequest : notnull ,  ICachedQuery
    {
        #region Fields
        private readonly ICacheService _cache;
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger; // use it for logging cache hits and misses
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
            _settings = settings.Value;     // because IOptions<CacheSettings> is wrapper on Cache settings, and we need the actual Cache Settings object
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
            // Cache HIT: Here Redis save data in memory
            if (cached is not null)
            {
                _logger.LogInformation("Cache hit for {CacheKey}", cacheKey);
                // Handler will NOT run, Database will NOT be accessed
                return cached;
            }

            // Redis do not save data in memory
            _logger.LogInformation("Cache miss for {CacheKey}", cacheKey);    

            // Cache miss -> Go to the Handler and execute the request
            var response = await next();

            // Store the result for the next request.
            // Store the result, for as long as appsettings.json says.
            var duration = _settings.GetDuration(request.Duration); // get the duration from appsettings.json based on the request.Duration enum value
            if (response is not null && duration > TimeSpan.Zero)
                await _cache.SetAsync(cacheKey, response, duration, cancellationToken);

            return response;
        }
        #endregion

        #region Private Methods
        // Builds the real key. If the response depends on the role, the role becomes part of the key, so an Admin response is never served to a User.
        private async Task<string> BuildKey(TRequest request, CancellationToken ct)                                  
        {
            var key = request.CacheKey;  // Start with the base cache key from the request

            // check if the request his response depends on the role.
            // if yes add the role to the key, so an Admin response is never served to a User.
            if (request is IVaryByRole)      
                key += _currentUserService.IsAdmin ? ":role:admin" : ":role:user";
            
            // Use version for all cached data in app.
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
//      GET / departments
//             ↓
//          MediatR
//             ↓
//       CachingBehavior
//             ↓
//         BuildKey()
//             ↓
//    ams:departments: list
//             ↓
//         Redis GET
//             ↓
//           MISS
//             ↓
//          next()
//             ↓
//   Department Handler
//             ↓
//          EF Core
//             ↓
//        SQL Server
//             ↓
//      Department DTOs
//             ↓
//      CachingBehavior
//             ↓
//       GetDuration()
//             ↓
//        Redis SET
//             ↓
//      Return response
#endregion