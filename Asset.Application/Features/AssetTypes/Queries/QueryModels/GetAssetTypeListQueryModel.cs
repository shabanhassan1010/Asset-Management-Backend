using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.AssetTypes.Queries.QueryModels
{
    public class GetAssetTypeListQueryModel: IRequest<BaseResponse<IReadOnlyList<GetAssetTypeListQueryResponse>>> , ICachedQuery
    {
        public string CacheKey => CacheKeys.AssetTypeList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);
    }
}
