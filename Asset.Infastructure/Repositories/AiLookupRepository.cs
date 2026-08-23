using Asset.Application.Interfaces.IRepository;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Asset.Infastructure.Repositories
{
    public class AiLookupRepository : IAiLookupRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        private const int MaxEmployeeMatches = 5;
        #endregion

        #region Constructor
        public AiLookupRepository(AssetManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Merthods
        public async Task<int?> GetAssetTypeIdByNameAsync(string typeName, CancellationToken ct)
        {
            // Exact match. SQL Server's default collation is case-insensitive,
            // so "laptop" and "Laptop" both find the same row without ToLower(),
            // which would prevent the index from being used.
            var id = await _dbContext.AssetTypes.AsNoTracking().Where(t => t.TypeName == typeName && t.IsActive)
                                                .Select(t => (int?)t.Id).FirstOrDefaultAsync(ct);

            return id;
        }
        public async Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct)
        {
            var id = await _dbContext.Departments.AsNoTracking().Where(d => (d.DepartmentName == departmentName || d.Code == departmentName) && d.IsActive)
                                                 .Select(d => (int?)d.Id).FirstOrDefaultAsync(ct);

            return id;
        }
        public async Task<List<EmployeeLookup>> FindEmployeesByNameAsync(string name, CancellationToken ct)
        {
            return await _dbContext.Employees.AsNoTracking().Where(e => e.FullName.Contains(name) && e.IsActive).OrderBy(e => e.FullName)
                                             .Take(MaxEmployeeMatches)
                                             .Select(e => new EmployeeLookup(e.Id, e.FullName)).ToListAsync(ct);
        }
        #endregion
    }
}
