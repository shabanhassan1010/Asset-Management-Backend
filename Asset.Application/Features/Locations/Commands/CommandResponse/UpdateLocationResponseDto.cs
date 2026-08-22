namespace Asset.Application.Features.Locations.Commands.CommandResponse
{
    public class UpdateLocationResponseDto
    {
        public string LocationName { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}
