#region 
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public DepartmentRepository(AssetManagementDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Methods

        // Get 
        public async Task<IReadOnlyList<GetDepartmentListResponse>> GetAllProjectedAsync(CancellationToken ct)
        {
            return await _dbContext.Departments
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DepartmentName)
                .Select(d => new GetDepartmentListResponse
                {
                    Id = d.Id,
                    DepartmentName = d.DepartmentName,
                    Code = d.Code,
                    AssetsCount = d.Assets.Count(),
                    EmployeesCount = d.Employees.Count()
                })
                .ToListAsync(ct);
        }

        // Count
        public async Task<int> CountEmployeesAsync(int departmentId, CancellationToken ct)
        {
            return await _dbContext.Employees
                        .AsNoTracking()
                        .CountAsync(e => e.DepartmentId == departmentId, ct);
        }
        public async Task<int> CountAssetsAsync(int departmentId, CancellationToken ct)
        {
            return await _dbContext.Assets
                                   .AsNoTracking()
                                   .CountAsync(a => a.DepartmentId == departmentId, ct);
        }

        // Check
        public async Task<bool> ExistsActiveAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Departments.AnyAsync(a => a.Id == id && a.IsActive, ct);
        }
        public Task<bool> IsNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Departments.AsNoTracking().Where(a => a.DepartmentName == name);
            if (exceptId.HasValue)
            {
                query = query.Where(a => a.Id != exceptId.Value);
            }
            return query.AnyAsync(ct);
        }
        public Task<bool> IsCodeExistsAsync(string code, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Departments.AsNoTracking().Where(a => a.Code == code);
            if (exceptId.HasValue)  // exceptId in create is null, in update it is the id of the asset being updated
            {
                query = query.Where(a => a.Id != exceptId.Value);
            }
            return query.AnyAsync(ct);
        }

        // Delete
        public void Remove(Department entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}
