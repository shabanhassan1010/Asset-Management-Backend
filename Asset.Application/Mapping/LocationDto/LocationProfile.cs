using AutoMapper;
namespace Asset.Application.Mapping.LocationDto
{
    public partial class LocationProfile : Profile
    {
        public LocationProfile()
        {
            CreateLocation();
            UpdateLocation();
            GetLocation();
        }
    }
}
