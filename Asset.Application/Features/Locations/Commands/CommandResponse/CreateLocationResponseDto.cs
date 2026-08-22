namespace Asset.Application.Features.Locations.Commands.CommandResponse
{
    public class CreateLocationResponseDto
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}
