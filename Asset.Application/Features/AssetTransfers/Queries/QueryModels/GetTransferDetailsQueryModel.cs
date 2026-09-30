using Asset.Application.Bases;
using Asset.Application.Features.AssetTransfers.Queries.QueryResponses;
using MediatR;

namespace Asset.Application.Features.AssetTransfers.Queries.QueryModels
{
    public record GetTransferDetailsQueryModel(int AssetId, int TransferId) : IRequest<BaseResponse<GetTransferDetailsResponse>>;
}