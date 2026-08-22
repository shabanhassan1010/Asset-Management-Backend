using MediatR;

namespace Asset.Application.Features.Users.Commands.ChangeUserStatus;

/// <summary>
/// One endpoint for both directions instead of /disable and /enable. The rules
/// are the same either way, and two endpoints would duplicate them.
/// </summary>
public record ChangeUserStatusCommand(string UserId, bool IsActive) : IRequest;
