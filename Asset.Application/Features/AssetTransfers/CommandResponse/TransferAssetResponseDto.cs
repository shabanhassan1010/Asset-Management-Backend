namespace Asset.Application.Features.AssetTransfers.CommandResponse
{
    /// RowVersion is returned because EF refreshes it during SaveChanges. Without it the client would hold a stale stamp and its very next transfer or edit
    /// would fail with a spurious 409, forcing a full re-fetch.
    public class TransferAssetResponseDto
    {
        public int TransferId { get; set; }

        public int AssetId { get; set; }
        public string AssetCode { get; set; }

        public int? ToEmployeeId { get; set; }
        public int? ToDepartmentId { get; set; }
        public int? ToLocationId { get; set; }

        public int Status { get; set; }

        public DateTime TransferDate { get; set; }
        public string RowVersion { get; set; }
    }
}
