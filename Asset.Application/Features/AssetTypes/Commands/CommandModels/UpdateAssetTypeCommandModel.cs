using Asset.Application.Bases;
using MediatR;
namespace Asset.Application.Features.AssetTypes.Commands.CommandModels
{
    public class UpdateAssetTypeCommandModel : IRequest<BaseResponse<string>>
    {
        public int Id { get; set; }
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}