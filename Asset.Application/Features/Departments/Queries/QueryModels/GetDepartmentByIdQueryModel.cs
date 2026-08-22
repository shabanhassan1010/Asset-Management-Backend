using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using MediatR;
namespace Asset.Application.Features.Departments.Queries.QueryModels
{
    public class GetDepartmentByIdQueryModel : IRequest<ApiResponse<GetDepartmentByIdResponse>> , ICachedQuery
    {
        public int Id { get; set; }

        public GetDepartmentByIdQueryModel(int id)
        {
            Id = id;
        }
        public string CacheKey => CacheKeys.DepartmentList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);
    }
}
