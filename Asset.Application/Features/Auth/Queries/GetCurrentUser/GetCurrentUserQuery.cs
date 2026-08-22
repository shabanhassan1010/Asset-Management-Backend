using Asset.Application.Features.Auth.DTOs;
using MediatR;

namespace Asset.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// No parameters: the identity comes from the token, never from the caller.
/// </summary>
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
