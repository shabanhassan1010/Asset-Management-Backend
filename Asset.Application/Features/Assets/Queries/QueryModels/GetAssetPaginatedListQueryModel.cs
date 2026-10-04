using Asset.Application.Common.Caching;
using Asset.Application.Features.Assets.Queries.QueryResponses;
using Asset.Domain.Common;
using MediatR;

namespace Asset.Application.Features.Assets.Queries.QueryModels
{
    public class GetAssetPaginatedListQueryModel : IRequest<PaginatedResponse<GetAssetPaginatedListQueryResponse>>,
                                                   ICachedQuery,      // cache this query
                                                   IVaryByRole,       // Admin sees PurchaseCost, User doesn't
                                                   IVersionedCache    // one version clears every list page
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public int? AssetTypeId { get; set; }

        public byte? StatusId { get; set; }

        public int? DepartmentId { get; set; }

        public int? LocationId { get; set; }

        public int? EmployeeId { get; set; }

        public bool IncludeRetired { get; set; } = false;

        public string? SortBy { get; set; }

        public bool SortDesc { get; set; } = false;
        // the key contains the FULL query shape: every filter, the sort and the page.
        // Two requests share a key only if they would return exactly the same rows.
        // Search is LAST: whatever the user types can't be confused with the other parts.
        public string CacheKey =>                                                         
            $"assets:list" +
            $":p{PageNumber}:s{PageSize}" +
            $":c{CategoryId}:t{AssetTypeId}:st{StatusId}" +
            $":d{DepartmentId}:l{LocationId}:e{EmployeeId}" +
            $":r{(IncludeRetired ? 1 : 0)}" +
            $":o{(SortBy ?? "assetcode").ToLowerInvariant()}:{(SortDesc ? "desc" : "asc")}" +
            $":q{Search?.Trim().ToLowerInvariant()}";

        public CacheDuration Duration => CacheDuration.Volatile;        //  short TTL
        public string VersionKey => CacheKeys.AssetsVersion;            //  same version as asset detail
    }
}
