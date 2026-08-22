using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Interfaces.IRepository
{
    public interface IAiLookupRepository
    {
        Task<int?> GetAssetTypeIdByNameAsync(string typeName, CancellationToken ct);
        Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct);
        Task<List<EmployeeLookup>> FindEmployeesByNameAsync(string name, CancellationToken ct);
    }
    public record EmployeeLookup(int Id, string FullName);
}
