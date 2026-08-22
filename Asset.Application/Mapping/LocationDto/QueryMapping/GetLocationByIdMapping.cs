using Asset.Application.Features.Locations.Queries.QueryResponse;
using Asset.Domain.Models;
namespace Asset.Application.Mapping.LocationDto
{
    public partial class LocationProfile
    {
        public void GetLocation()
        {
            CreateMap<Location, GetLocationByIdResponse>();
        }
    }
}
