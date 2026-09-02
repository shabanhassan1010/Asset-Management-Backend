using Asset.Application.Features.Auth.DTOs;
using MediatR;

namespace Asset.Application.Features.Auth.Queries.GetCurrentUser;
public record GetCurrentUserQuery : IRequest<CurrentUserDto>;
