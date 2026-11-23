namespace Asset.Application.Features.AssetTypes.Queries.QueryResponses
{
    public class GetAssetTypeListQueryResponse
    {
        public int Id { get; set; }
        public string? Description { get; set; }
        public string AssetTypeName { get; set; } = string.Empty;
        public string? AssetTypeNameAr { get; set; }
        public int AssetsCount { get; set; }
        public bool IsActive { get; set; }
    }
}