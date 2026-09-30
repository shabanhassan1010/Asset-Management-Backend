using Asset.Application.Bases;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AssetTransfers.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.AssetTransfers.Queries.QueryModels
{
    public class GetAssetTransferHistoryQueryModel: IRequest<BaseResponse<IReadOnlyList<GetAssetTransferHistoryResponse>>>
    {
        public int AssetId { get; set; }

        public GetAssetTransferHistoryQueryModel(int assetId)
        {
            AssetId = assetId;
        }
    }
}
