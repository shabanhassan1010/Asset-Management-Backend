using Asset.Application.Common.Caching;
using Asset.Application.Features.Assets.DTOs;
using Asset.Application.Features.Assets.Queries.QueryResponses;
using MediatR;
namespace Asset.Application.Features.Assets.Queries.QueryModels
{
    public class GetAssetByIdQueryModel : IRequest<GetByIdQueryResponse> , 
                                                            ICachedQuery,    // cache this query
                                                            IVaryByRole,     // Admin sees PurchaseCost, User doesn't
                                                            IVersionedCache //  one version clears all asset keys
    {
        public int Id { get; set; }
        public GetAssetByIdQueryModel(int id)
        {
            Id = id;
        }
        public string CacheKey => CacheKeys.AssetById(Id);              // "assets:5"
        public CacheDuration Duration => CacheDuration.Volatile;        // short TTL (VolatileMinutes)
        public string VersionKey => CacheKeys.AssetsVersion;            // "assets:version"

    }
}
