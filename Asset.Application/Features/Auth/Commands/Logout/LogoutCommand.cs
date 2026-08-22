using MediatR;

namespace Asset.Application.Features.Auth.Commands.Logout;

/// <summary>
/// IRequest with no generic argument: logout returns nothing, so the endpoint
/// answers 204 No Content.
/// </summary>
public record LogoutCommand(string RefreshToken) : IRequest;
