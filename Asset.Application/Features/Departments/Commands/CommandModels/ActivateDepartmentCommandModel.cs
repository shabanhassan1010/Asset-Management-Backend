using Asset.Application.Common.Responses;
using MediatR;

namespace Asset.Application.Features.Departments.Commands.CommandModels
{
    // Turns a deactivated department back on (IsActive = true). The opposite of the DELETE endpoint.
    // A record: it only carries the id, and two requests with the same id are equal.
    public record ActivateDepartmentCommandModel(int Id) : IRequest<ApiResponse<string>>;
}
