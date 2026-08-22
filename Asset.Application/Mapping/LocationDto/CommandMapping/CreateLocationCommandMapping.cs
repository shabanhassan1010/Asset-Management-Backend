using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using Asset.Domain.Models;

namespace Asset.Application.Mapping.LocationDto
{
    public partial class LocationProfile
    {
        public void CreateLocation()
        {
            CreateMap<CreateLocationCommandModel, Location>();
            CreateMap<Location, CreateLocationResponseDto>();
        }
    }
}
