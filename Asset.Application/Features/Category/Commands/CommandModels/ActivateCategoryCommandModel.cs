using Asset.Application.Common.Responses;
using MediatR;

namespace Asset.Application.Features.Category.Commands.CommandModels
{
    // Turns a deactivated category back on (IsActive = true). The opposite of the DELETE endpoint.
    // A record: it only carries the id, and two requests with the same id are equal.
    public record ActivateCategoryCommandModel(int Id) : IRequest<ApiResponse<string>>;
}
