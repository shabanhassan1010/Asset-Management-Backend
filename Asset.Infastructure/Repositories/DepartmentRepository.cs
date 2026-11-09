#region 
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using Asset.Application.Features.Departments.Queries.QueryResponse;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Enum;
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
            return await _dbContext.Departments.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.DepartmentName)
                                                .Select(d => new GetDepartmentListResponse
                                                {
                                                    Id = d.Id,
                                                    DepartmentName = d.DepartmentName,
                                                    Code = d.Code,
                                                    IsActive = d.IsActive,
                                                    AssetsCount = d.Assets.Count(a => a.Status != (int)AssetStatus.Retired),
                                                    EmployeesCount = d.Employees.Count(e => e.IsActive)
                                                }).ToListAsync(ct);
        }
        public async Task<(List<GetDepartmentListResponse> Items, int TotalCount)> GetPagedAsync(string? search, bool? isActive, int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.Departments.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.DepartmentName.Contains(search));
            }

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderBy(t => t.DepartmentName)
                .ThenBy(t => t.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new GetDepartmentListResponse
                {
                    Id = t.Id,
                    DepartmentName = t.DepartmentName,
                    Code = t.Code,
                    IsActive = t.IsActive,
                    AssetsCount = t.Assets.Count(a => a.Status != (int)AssetStatus.Retired),
                    EmployeesCount = t.Employees.Count(e => e.IsActive)
                })
                .ToListAsync(ct);

            return (items, totalCount);
        }
        // Check
        public async Task<bool> ExistsActiveAsync(int id, CancellationToken ct)
        {
            return await _dbContext.Departments.AnyAsync(a => a.Id == id && a.IsActive, ct);
        }
        public Task<bool> IsNameExistsAsync(string name, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Departments.AsNoTracking().Where(a => a.DepartmentName == name && a.IsActive);
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
        public void DeActivate(Department entity)
        {
            entity.IsActive = false;
        }
        #endregion
    }
}
