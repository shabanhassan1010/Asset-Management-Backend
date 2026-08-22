namespace Asset.Application.Features.AssetTypes.Queries.QueryResponses
{
    public class GetAssetTypeListQueryResponse
    {
        public int Id { get; set; }
        public string AssetTypeName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
