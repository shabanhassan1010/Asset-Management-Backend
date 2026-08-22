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

        // R2.6: التكلفة لازم تبقى **غايبة** من الـ response لليوزر مش null.
        // WhenWritingNull بيشيل المفتاح نفسه من الـ JSON.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PortfolioValue { get; init; }

        public IReadOnlyList<CategoryCountDto> AssetsByCategory { get; init; } = [];
        public IReadOnlyList<ExpiringWarrantyDto> ExpiringWarranties { get; init; } = [];
    }    
    public record CategoryCountDto(string CategoryName, int Count);
    public record AssetStatusCounts(int Active, int Retired,int Available,int Assigned,int UnderMaintenance);
    public record ExpiringWarrantyDto(int AssetId, string AssetCode, string AssetName, int StatusId, string StatusName, DateOnly WarrantyExpiryDate);
}
