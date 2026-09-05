using Asset.Application.Bases;
using MediatR;
namespace Asset.Application.Features.AssetTypes.Commands.CommandModels
{
    public class UpdateAssetTypeCommandModel : IRequest<BaseResponse<string>>
    {
        public int Id { get; set; }
        public string assetTypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}