namespace Asset.Application.Interfaces.IRepository
{
    public interface IAiLookupRepository
    {
        Task<int?> GetAssetTypeIdByNameAsync(string typeName, CancellationToken ct);
        Task<int?> GetDepartmentIdByNameAsync(string departmentName, CancellationToken ct);
        Task<List<EmployeeLookup>> FindEmployeesByNameAsync(string name, CancellationToken ct);

        // Sent to Gemini so it picks a real name instead of guessing one.
        Task<List<string>> GetAssetTypeNamesAsync(CancellationToken ct);
        Task<List<string>> GetDepartmentNamesAsync(CancellationToken ct);
    }
    public record EmployeeLookup(int Id, string FullName);
}