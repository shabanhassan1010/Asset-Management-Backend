using Asset.Application.Common.Responses;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Employees.Queries.QueryModels
{
    public class GetEmployeesPaginatedQuery : IRequest<ApiResponse<PagedResult<GetEmployeeListQueryResponse>>>
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }   
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
    }
}
