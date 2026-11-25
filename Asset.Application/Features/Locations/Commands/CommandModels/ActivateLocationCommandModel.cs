using Asset.Application.Common.Responses;
using MediatR;

namespace Asset.Application.Features.Locations.Commands.CommandModels
{
    // Turns a deactivated location back on (IsActive = true). The opposite of the DELETE endpoint.
    // A record: it only carries the id, and two requests with the same id are equal.
    public record ActivateLocationCommandModel(int Id) : IRequest<ApiResponse<string>>;
}
