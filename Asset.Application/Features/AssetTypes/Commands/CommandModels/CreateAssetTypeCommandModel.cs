using Asset.Application.Bases;
using MediatR;
namespace Asset.Application.Features.AssetTypes.Commands.CommandModels
{
    public class CreateAssetTypeCommandModel : IRequest<BaseResponse<int>>
    {
        public string assetTypeName { get; set; } = string.Empty;
        public string? assetTypeNameAr { get; set; }     // optional
        public string? Description { get; set; }
    }
}