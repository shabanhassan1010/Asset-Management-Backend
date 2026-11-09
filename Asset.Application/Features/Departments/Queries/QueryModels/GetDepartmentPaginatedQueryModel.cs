using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using MediatR;

namespace Asset.Application.Features.Departments.Queries.QueryModels
{
    public class GetDepartmentPaginatedQueryModel : IRequest<ApiResponse<PagedResult<GetDepartmentListResponse>>>, ICachedQuery, IVersionedCache
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string CacheKey => $"DepartmentList:list" +
                                  $":p{PageNumber}:s{PageSize}" +
                                  $":q{Search?.Trim().ToLowerInvariant()}" +
                                  $":a{(IsActive.HasValue ? (IsActive.Value ? "1" : "0") : "all")}";
        public CacheDuration Duration => CacheDuration.Lookup;
        public string VersionKey => CacheKeys.DepartmentsVersion;            //  same version as asset detail
    }
}
