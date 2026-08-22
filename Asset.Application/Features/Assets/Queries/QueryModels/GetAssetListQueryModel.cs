using Asset.Application.Features.Assets.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Assets.Queries.QueryModels
{
    public class GetAssetListQueryModel : IRequest<List<GetAssetListQueryResponse>>
    {
    }
}
