using MediatR;
namespace Asset.Application.Features.Auth.Commands.Logout;
public record LogoutCommand(string RefreshToken) : IRequest;
