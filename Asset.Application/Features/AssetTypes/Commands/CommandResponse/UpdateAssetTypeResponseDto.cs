namespace Asset.Application.Features.AssetTypes.Commands.CommandResponse
{
    public class UpdateAssetTypeResponseDto
    {
        public int Id { get; set; }
        public string TypeName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}