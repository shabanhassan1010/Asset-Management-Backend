using Asset.Application.Bases;
using Asset.Application.Common.Caching;
using Asset.Application.Features.AssetTypes.Queries.QueryResponses;
using MediatR;

namespace Asset.Application.Features.AssetTypes.Queries.QueryModels
{
    public class GetAssetTypeByIdQueryModel : IRequest<BaseResponse<GetAssetTypeByIdQueryResponse>> , ICachedQuery
    {
        public int Id { get; set; }
        public string CacheKey => CacheKeys.AssetTypeById(Id);
        public TimeSpan Duration => CacheDurations.Lookup;
    }
}