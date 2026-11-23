using Asset.Application.Common;

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
    // FullNameAr is NULL when no Arabic name was entered.
    public record EmployeeLookup(int Id, string FullName, string? FullNameAr)
    {
        // The name to show in the chat, in the language of the request.
        public string DisplayName => LocalizedName.Pick(FullName, FullNameAr);
    }
}