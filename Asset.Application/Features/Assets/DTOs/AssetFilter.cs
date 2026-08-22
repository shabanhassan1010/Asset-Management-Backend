namespace Asset.Application.Features.Assets.DTOs
{
    // Everything the list screen can filter and sort by (R2.2).
    // The controller binds it straight from the query string with [FromQuery].
    public class AssetFilter
    {
        public string? Search { get; set; }          // matches AssetCode, AssetName or SerialNumber
        public int? CategoryId { get; set; }
        public int? AssetTypeId { get; set; }
        public byte? StatusId { get; set; }
        public int? DepartmentId { get; set; }
        public int? LocationId { get; set; }
        public int? EmployeeId { get; set; }
        public bool IncludeRetired { get; set; }
        public string? Manufacturer { get; set; }
        public string SortBy { get; set; } = "AssetCode";   // AssetCode | AssetName | PurchaseDate
        public bool SortDesc { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
