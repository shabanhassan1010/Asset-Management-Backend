using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Domain.Exceptions;
using AutoMapper;
using MediatR;

namespace Asset.Application.Features.Auth.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;

    public GetCurrentUserQueryHandler(IUserRepository users, ICurrentUserService currentUser, IMapper mapper)
    {
        _users = users;
        _currentUser = currentUser;
        _mapper = mapper;
    }

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        // Read from the database, not from the claims. The token was minted at
        // sign-in; the role or the account status may have changed since. The
        // Angular shell trusts this endpoint, so it has to be current.
        var user = await _users.GetByIdAsync(_currentUser.UserId!, cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException("This account is no longer active.");

        var role = await _users.GetRoleAsync(user, cancellationToken);

        return _mapper.Map<CurrentUserDto>(new UserWithRole(user, role));
    }
}
