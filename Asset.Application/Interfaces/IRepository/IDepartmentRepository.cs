using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Models;

namespace Asset.Application.Interfaces.IRepository
{
    public interface IDepartmentRepository : IBaseRepository<Department> , 
                                             IActiveRepository<Department>,
                                             ICheckRepository<Department>
    {
        // Read
        Task<IReadOnlyList<GetDepartmentListResponse>> GetAllProjectedAsync(CancellationToken ct);
        Task<(List<GetDepartmentListResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct);

        // Write
        void DeActivate(Department entity);
    }
}
