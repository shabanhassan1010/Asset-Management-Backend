using Asset.Application.Common.Responses;
using MediatR;

namespace Asset.Application.Features.AssetTypes.Commands.CommandModels
{
    // Turns a deactivated asset type back on (IsActive = true). The opposite of the DELETE endpoint.
    // A record: it only carries the id, and two requests with the same id are equal.
    public record ActivateAssetTypeCommandModel(int Id) : IRequest<ApiResponse<string>>;
}
