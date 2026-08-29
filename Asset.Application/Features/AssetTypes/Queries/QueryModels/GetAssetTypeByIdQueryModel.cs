using Asset.Application.Bases;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using MediatR;

namespace Asset.Application.Features.AssetTypes.Queries.QueryModels
{
    public class GetAssetTypeByIdQueryModel : IRequest<BaseResponse<GetAssetTypeByIdQueryResponse>>
    {
        public int Id { get; set; }
    }
}