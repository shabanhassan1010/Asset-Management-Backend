#region
using Asset.Application.Features.Employees.DTos;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Models;
using Asset.Infastructure.Models;
using Microsoft.EntityFrameworkCore;
#endregion

namespace Asset.Infastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        #region Fields
        private readonly AssetManagementDbContext _dbContext;
        #endregion

        #region Constructor
        public EmployeeRepository(AssetManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region Methods

        // Get
        public Task<Employee?> GetByIdAsync(int id, CancellationToken ct)
        {
            // AsNoTracking: the row is read to validate a rule and then thrown away.
            // Tracking it would put an entity in the change tracker that SaveChanges then has to scan for modifications that cannot exist.
            return _dbContext.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        }
        public async Task<EmployeeInfo?> GetProjectedByIdAsync(int id, CancellationToken cancellationToken)
        {
            return await _dbContext.Employees
                .Where(e => e.Id == id)
                .Select(e => new EmployeeInfo
                {
                    Id = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    FullName = e.FullName,
                    Email = e.Email,
                    Phone = e.Phone,
                    DepartmentId = e.DepartmentId,
                    DepartmentName = e.Department.DepartmentName,
                    IsActive = e.IsActive
                })
                .FirstOrDefaultAsync(cancellationToken);
        }
        public Task<Employee?> GetByIdWithDepartmentAsNoTrackingAsync(int id, CancellationToken ct)
        {
            return _dbContext.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.Id == id, ct);
        }
        public async Task<IReadOnlyList<Employee>> GetAllWithDepartmentAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.Employees.AsNoTracking().Include(e => e.Department).OrderBy(e => e.FullName).ToListAsync(cancellationToken);
        }                 
        public async Task<IReadOnlyList<Employee>> GetAvailableAsync(IReadOnlyList<int> takenEmployeeIds,int? departmentId ,CancellationToken cancellationToken)
        {
            var query = _dbContext.Employees.AsNoTracking().Where(e => !takenEmployeeIds.Contains(e.Id));

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            return await query.OrderBy(e => e.FullName).ToListAsync(cancellationToken);
        }
        public async Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(string search, int? departmentId, bool? isActive,int pageNumber, int pageSize, CancellationToken ct)
        {
            var query = _dbContext.Employees.AsNoTracking().Include(e => e.Department)  .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                query = query.Where(e => e.FullName.Contains(search) || e.EmployeeCode.Contains(search));
            }

            if (departmentId.HasValue)
                query = query.Where(e => e.DepartmentId == departmentId.Value);

            if (isActive.HasValue)
                query = query.Where(e => e.IsActive == isActive.Value);

            var totalCount = await query.CountAsync(ct);

            var items = await query.OrderBy(e => e.FullName).ThenBy(e => e.Id)
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync(ct);

            return (items, totalCount);
        }

        // Check
        public Task<bool> ExistsAsync(int employeeId, CancellationToken cancellationToken)
        {
            return _dbContext.Employees.AnyAsync(e => e.Id == employeeId, cancellationToken);
        }
        public Task<bool> IsEmailExistsAsync(string email, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Employees.AsNoTracking().Where(e => e.Email == email);

            if (exceptId.HasValue)
            {
                query = query.Where(e => e.Id != exceptId.Value);
            }

            return query.AnyAsync(ct);
        }
        public Task<bool> IsCodeExistsAsync(string employeeCode, int? exceptId, CancellationToken ct)
        {
            var query = _dbContext.Employees.AsNoTracking().Where(e => e.EmployeeCode == employeeCode);

            if (exceptId.HasValue)  // exceptId in create is null, in update it is the id of the employee being updated
            {
                query = query.Where(e => e.Id != exceptId.Value);
            }

            return query.AnyAsync(ct);
        }
        public Task<bool> HasAssignedAssetsAsync(int employeeId, CancellationToken ct)
        {
            return _dbContext.Assets.AsNoTracking().AnyAsync(a => a.AssignedEmployeeId == employeeId, ct);
        }

        // Add
        public async Task AddAsync(Employee employee, CancellationToken ct)
        {
            await _dbContext.Employees.AddAsync(employee, ct);
        }
        #endregion
    }
}