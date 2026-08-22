using Asset.Domain.Enum;
using Asset.Domain.Identity;
using MediatR;

namespace Asset.Application.Features.Users.Commands.ChangeUserRole;

/// <summary>
/// UserId comes from the route, Role from the body. The controller assembles
/// both, so the handler never has to know where each value came from.
/// </summary>
public record ChangeUserRoleCommand(string UserId, Role Role) : IRequest;
