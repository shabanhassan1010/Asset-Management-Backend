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

        // Write
        void Remove(Department entity);
    }
}
