using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using MediatR;

namespace Asset.Application.Features.Locations.Queries.QueryModels
{
    public class GetLocationPaginatedModelQuery : IRequest<ApiResponse<PagedResult<GetLocationListResponse>>>, ICachedQuery, IVersionedCache
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string CacheKey => $"locations:list" +
                                  $":p{PageNumber}:s{PageSize}" +
                                  $":q{Search?.Trim().ToLowerInvariant()}" +
                                  $":a{(IsActive.HasValue ? (IsActive.Value ? "1" : "0") : "all")}";
        public CacheDuration Duration => CacheDuration.Lookup;
        public string VersionKey => CacheKeys.LocationsVersion;            //  same version as asset detail
    }
}
