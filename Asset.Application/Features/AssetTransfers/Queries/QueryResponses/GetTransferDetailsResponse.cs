namespace Asset.Application.Features.AssetTransfers.Queries.QueryResponses
{
    public class GetTransferDetailsResponse
    {
        public int Id { get; set; }
        public DateTime TransferDate { get; set; }
        public string? Reason { get; set; }

        // Asset
        public int AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetCode { get; set; } = string.Empty;
        public string? SerialNumber { get; set; }

        // From
        public string? FromEmployeeName { get; set; }
        public string? FromDepartmentName { get; set; }
        public string? FromLocationName { get; set; }

        // To
        public string? ToEmployeeName { get; set; }
        public string? ToDepartmentName { get; set; }
        public string? ToLocationName { get; set; }

        // Admin who made the transfer (comes from the Identity DbContext)
        public string? TransferredByName { get; set; }
    }
}
