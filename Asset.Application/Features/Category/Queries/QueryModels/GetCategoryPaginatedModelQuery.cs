using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Category.Queries.QueryResponse;
using MediatR;

namespace Asset.Application.Features.Category.Queries.QueryModels
{
    public class GetCategoryPaginatedModelQuery : IRequest<ApiResponse<PagedResult<GetCategoryListResponse>>>, ICachedQuery, IVersionedCache
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string CacheKey => $"categories:list" +
                                  $":p{PageNumber}:s{PageSize}" +
                                  $":q{Search?.Trim().ToLowerInvariant()}" +
                                  $":a{(IsActive.HasValue ? (IsActive.Value ? "1" : "0") : "all")}";
        public CacheDuration Duration => CacheDuration.Lookup;
        public string VersionKey => CacheKeys.CategoriesVersion;            //  same version as asset detail
    }
}
