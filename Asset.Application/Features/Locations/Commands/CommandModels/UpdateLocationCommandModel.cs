using Asset.Application.Common.Responses;
using Asset.Application.Features.Locations.Commands.CommandResponse;
using MediatR;
namespace Asset.Application.Features.Locations.Commands.CommandModels
{
    public class UpdateLocationCommandModel : IRequest<ApiResponse<UpdateLocationResponseDto>>
    {
        public int Id { get; set; }
        public string LocationName { get; set; }
        public string Address { get; set; }
    }
}
