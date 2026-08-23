using Asset.Domain.Enum;
using MediatR;

namespace Asset.Application.Features.Users.Commands.ChangeUserRole;
public record ChangeUserRoleCommand(string UserId, Role Role) : IRequest;
