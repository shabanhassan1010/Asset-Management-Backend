namespace Asset.Application.Features.Assets.DTOs
{
    // One row of the history (R3.2). It never changes after it is written.
    public class AssetTransferDto
    {
        public long Id { get; set; }
        public string? FromEmployeeName { get; set; }
        public string? FromDepartmentName { get; set; }
        public string? FromLocationName { get; set; }
        public string? ToEmployeeName { get; set; }
        public string? ToDepartmentName { get; set; }
        public string? ToLocationName { get; set; }
        public DateOnly TransferDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
    }

}
