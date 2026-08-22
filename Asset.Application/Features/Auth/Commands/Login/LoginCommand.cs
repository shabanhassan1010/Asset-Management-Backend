using Asset.Application.Features.Auth.DTOs;
using MediatR;

namespace Asset.Application.Features.Auth.Commands.Login;

/// <summary>
/// A record because the request is an immutable message: bound once, read
/// once, never mutated in between.
/// </summary>
public record LoginCommand(string UserName, string Password) : IRequest<AuthResponseDto>;
