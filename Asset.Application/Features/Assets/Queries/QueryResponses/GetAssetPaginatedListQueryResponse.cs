using System.Text.Json.Serialization;

namespace Asset.Application.Features.Assets.Queries.QueryResponses
{
    public class GetAssetPaginatedListQueryResponse
    {
        public int Id { get; set; }
        public string AssetCode { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int AssetTypeId { get; set; }
        public string AssetTypeName { get; set; } = string.Empty;
        public byte StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }

        public DateOnly? PurchaseDate { get; set; }
        public DateOnly? WarrantyExpiryDate { get; set; }

        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int? LocationId { get; set; }
        public string? LocationName { get; set; }
        // Admin only.
        // The handler sets it to null for a User, and WhenWritingNull means the key
        // is then NOT written into the JSON at all - so it is absent, not just empty.
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public decimal? PurchaseCost { get; set; }
        public string RowVersion { get; set; }
    }
}
