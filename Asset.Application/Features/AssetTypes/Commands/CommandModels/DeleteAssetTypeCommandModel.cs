using Asset.Application.Bases;
using MediatR;
namespace Asset.Application.Features.AssetTypes.Commands.CommandModels
{
    public class DeleteAssetTypeCommandModel : IRequest<BaseResponse<string>>
    {
        public int Id { get; set; }
    }
}