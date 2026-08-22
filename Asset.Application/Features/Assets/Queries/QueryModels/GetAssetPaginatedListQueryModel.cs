using Asset.Application.Features.Assets.Queries.QueryResponses;
using Asset.Domain.Common;
using MediatR;

namespace Asset.Application.Features.Assets.Queries.QueryModels
{
    public class GetAssetPaginatedListQueryModel : IRequest<PaginatedResponse<GetAssetPaginatedListQueryResponse>>
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
    }
}
