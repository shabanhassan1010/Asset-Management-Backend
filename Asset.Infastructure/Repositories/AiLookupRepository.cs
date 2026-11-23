using Asset.Application.Interfaces.IRepository;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Asset.Infastructure.Repositories
{
    public class AiLookupRepository : IAiLookupRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        private const int MaxEmployee = 5;
        #endregion

        #region Constructor
        public AiLookupRepository(AssetManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Methods
        public async Task<int?> GetAssetTypeIdByNameAsync(string typeName, CancellationToken ct)
        {
            var id = await _dbContext.AssetTypes.AsNoTracking()
                                                // The English OR the Arabic name: "Laptop" and "لابتوب" find the same row.
                                                .Where(t => (t.TypeName == typeName || t.TypeNameAr == typeName) && t.IsActive)
          // I use cast to int? to handle the case where no matching record is found, so that FirstOrDefaultAsync returns null instead of zero 
                                                .Select(t => (int?)t.Id)  
                                                .FirstOrDefaultAsync(ct);

            return id;
        }
        public async Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct)
        {
            var id = await _dbContext.Departments.AsNoTracking()
                                                 .Where(d => (d.DepartmentName == departmentName ||
                                                              d.DepartmentNameAr == departmentName ||
                                                              d.Code == departmentName) && d.IsActive)
                                                 .Select(d => (int?)d.Id)
                                                 .FirstOrDefaultAsync(ct);

            return id;
        }
        public async Task<List<EmployeeLookup>> FindEmployeesByNameAsync(string name, CancellationToken ct)
        {
            return await _dbContext.Employees.AsNoTracking()
                                             .Where(e => (e.FullName.Contains(name) ||
                                                          (e.FullNameAr != null && e.FullNameAr.Contains(name))) && e.IsActive)
                                             .OrderBy(e => e.FullName)
                                             .Take(MaxEmployee)
                                             .Select(e => new EmployeeLookup(e.Id, e.FullName, e.FullNameAr)).ToListAsync(ct);
        }

        public async Task<List<string>> GetAssetTypeNamesAsync(CancellationToken ct)
        {
            return await _dbContext.AssetTypes.AsNoTracking()
                                              .Where(t => t.IsActive)
                                              .OrderBy(t => t.TypeName)
                                              .Select(t => t.TypeName)
                                              .ToListAsync(ct);
        }

        public async Task<List<string>> GetDepartmentNamesAsync(CancellationToken ct)
        {
            return await _dbContext.Departments.AsNoTracking()
                                               .Where(d=>d.IsActive)
                                               .OrderBy(d => d.DepartmentName)
                                               .Select(d => d.DepartmentName + " (" + d.Code + ")")
                                               .ToListAsync(ct);
        }
        #endregion
    }
}