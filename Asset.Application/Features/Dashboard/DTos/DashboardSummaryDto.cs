using System.Text.Json.Serialization;

namespace Asset.Application.Features.Dashboard.DTos
{
    public record DashboardSummaryDto
    {
        public int ActiveAssets { get; init; }
        public int RetiredAssets { get; init; }
        public int AvailableAssets { get; init; }
        public int AssignedAssets { get; init; }
        public int UnderMaintenanceAssets { get; init; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PortfolioValue { get; init; }


        public IReadOnlyList<CategoryCountDto> AssetsByCategory { get; init; } = [];
        public IReadOnlyList<DepartmentCountDto> AssetsByDepartment { get; init; } = [];
        public IReadOnlyList<WarrantyWindowDto> WarrantyWindows { get; init; } = [];
        public IReadOnlyList<ExpiringWarrantyDto> ExpiringWarranties { get; init; } = [];
    }
    public record CategoryCountDto(int CategoryId, string CategoryName, int Count);
    public record DepartmentCountDto(int? DepartmentId, string DepartmentName, int Count);
    public record WarrantyWindowDto(int Days, int Count);
    public record AssetStatusCounts(int Active, int Retired,int Available,int Assigned,int UnderMaintenance);
    public record ExpiringWarrantyDto(int AssetId, string AssetCode, string AssetName, int StatusId, string StatusName, DateOnly WarrantyExpiryDate);
}
