using Asset.Application.Features.Employees.DTos;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Domain.Models;
namespace Asset.Application.Interfaces.IRepository
{
    public interface IEmployeeRepository
    {
        // Get
        Task<Employee?> GetByIdAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<Employee>> GetLookupAsync(CancellationToken cancellationToken);
        Task<EmployeeInfo?> GetProjectedByIdAsync(int id, CancellationToken cancellationToken);
        Task<Employee?> GetByIdWithDepartmentAsNoTrackingAsync(int id, CancellationToken ct);
        Task<IReadOnlyList<Employee>> GetAllWithDepartmentAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<Employee>> GetAvailableAsync(IReadOnlyList<int> takenEmployeeIds, int? departmentId, CancellationToken cancellationToken);
        Task<(List<Employee> Items, int TotalCount)> GetPagedAsync(string search, int? departmentId, bool? isActive, int pageNumber, int pageSize, CancellationToken ct);
        // Check
        Task<bool> IsEmailExistsAsync(string email, int? exceptId, CancellationToken ct);
        Task<bool> ExistsAsync(int employeeId, CancellationToken cancellationToken);
        Task<bool> IsCodeExistsAsync(string employeeCode, int? exceptId, CancellationToken ct);
        Task<bool> HasAssignedAssetsAsync(int employeeId, CancellationToken ct);

        // Add
        Task AddAsync(Employee employee, CancellationToken ct);
    }
}
