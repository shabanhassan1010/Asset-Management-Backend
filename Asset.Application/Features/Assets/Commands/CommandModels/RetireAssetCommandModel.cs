using Asset.Application.Common.Responses;
using Asset.Application.Features.Assets.Commands.CommandResponse;
using MediatR;
namespace Asset.Application.Features.Assets.Commands.CommandModels
{
    public class RetireAssetCommandModel : IRequest<ApiResponse<RetireAssetResponseDto>>
    {
        public int AssetId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string RowVersion { get; set; } = string.Empty;
    }
}
