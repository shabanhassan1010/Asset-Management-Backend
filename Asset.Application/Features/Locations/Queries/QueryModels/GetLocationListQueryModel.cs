using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using MediatR;
namespace Asset.Application.Features.Locations.Queries.QueryModels
{
    public class GetLocationListQueryModel : IRequest<ApiResponse<List<GetLocationListResponse>>>, ICachedQuery
    {
        public string CacheKey => CacheKeys.LocationList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);
    }
}
