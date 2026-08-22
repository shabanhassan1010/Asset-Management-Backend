using Asset.Application.Common.Caching;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Queries.QueryResponse;
using MediatR;
namespace Asset.Application.Features.Locations.Queries.QueryModels
{
    public class GetLocationByIdQueryModel : IRequest<ApiResponse<GetLocationByIdResponse>> , ICachedQuery
    {
        public int Id { get; set; }

        public GetLocationByIdQueryModel(int id)
        {
            Id = id;
        }
        public string CacheKey => CacheKeys.LocationList;
        public TimeSpan Duration => TimeSpan.FromMinutes(30);

    }
}
