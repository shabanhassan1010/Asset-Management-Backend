using MediatR;
namespace Asset.Application.Features.Users.Commands.ChangeUserStatus;
public record ChangeUserStatusCommand(string UserId, bool IsActive) : IRequest;
