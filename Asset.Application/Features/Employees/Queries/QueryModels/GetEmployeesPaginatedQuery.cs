#region
using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
#endregion
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeesPaginatedQuery : IRequest<ApiResponse<PagedResult<GetEmployeeListQueryResponse>>>, ICachedQuery, IVersionedCache
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }   
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
        public string CacheKey => $"employees:list" +
                                  $":p{PageNumber}:s{PageSize}" + 
                                  $":d{DepartmentId}" +
                                  $":q{Search?.Trim().ToLowerInvariant()}";
        public CacheDuration Duration => CacheDuration.Lookup;
        public string VersionKey => CacheKeys.AssetsVersion;            //  same version as asset detail
    }
}
