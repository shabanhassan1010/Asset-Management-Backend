using Asset.Application.Features.Auth.DTOs;
using MediatR;

namespace Asset.Application.Features.Auth.Commands.Refresh;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponseDto>;
