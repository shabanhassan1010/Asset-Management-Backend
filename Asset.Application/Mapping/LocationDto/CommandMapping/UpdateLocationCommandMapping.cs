using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using Asset.Domain.Models;
namespace Asset.Application.Mapping.LocationDto
{
    public partial class LocationProfile
    {
        public void UpdateLocation()
        {
            CreateMap<UpdateLocationCommandModel, Location>()
                .ForMember(d => d.Id, opt => opt.Ignore());

            CreateMap<Location, UpdateLocationResponseDto>();
        }
    }
}
