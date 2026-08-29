namespace Asset.Application.Features.AssetTypes.Queries.QueryResponses
{
    public class GetAssetTypeByIdQueryResponse
    {
        public int Id { get; set; }
        public string AssetTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}