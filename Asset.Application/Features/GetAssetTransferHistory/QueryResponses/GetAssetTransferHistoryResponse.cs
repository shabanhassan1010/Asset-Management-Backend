namespace Asset.Application.Features.GetAssetTransferHistory.QueryResponses
{
    public class GetAssetTransferHistoryResponse
    {
        public int Id { get; set; }
        public DateTime TransferDate { get; set; }

        // nullable صراحةً: التحويل ممكن يكون من "لا مكان" أو لـ "لا أحد"،
        // والـ non-nullable string كان هيدي تحذيرات ويخفي النية الحقيقية.
        public string? Reason { get; set; }
        public string? TransferredByUserId { get; set; }

        public string? FromEmployeeName { get; set; }
        public string? FromDepartmentName { get; set; }
        public string? FromLocationName { get; set; }

        public string? ToEmployeeName { get; set; }
        public string? ToDepartmentName { get; set; }
        public string? ToLocationName { get; set; }
    }
}
