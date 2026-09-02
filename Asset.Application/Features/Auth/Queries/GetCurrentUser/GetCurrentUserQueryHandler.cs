#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Features.Employees.DTos;
using Asset.Application.Interfaces.Comman;
using Asset.Domain.Exceptions;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion
namespace Asset.Application.Features.Auth.Queries.GetCurrentUser;
public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    #region Fields
    private readonly IUserRepository _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    #endregion

    #region Constructor
    public GetCurrentUserQueryHandler(IUserRepository users, ICurrentUserService currentUser, IMapper mapper, IUnitOfWork unitOfWork)
    {
        _users = users;
        _currentUser = currentUser;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }
    #endregion

    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(_currentUser.UserId!, cancellationToken);

        if (user is null || !user.IsActive)
            throw new AuthenticationFailedException("This account is no longer active.");

        var role = await _users.GetRoleAsync(user, cancellationToken);

        EmployeeInfo employee = await _unitOfWork.Employees.GetProjectedByIdAsync(user.EmployeeId.Value, cancellationToken);
        if(employee.Id == null)
            throw new AuthenticationFailedException("Employee information not found for the current user.");
        
        return _mapper.Map<CurrentUserDto>(new UserWithRole(user, role, employee));
    }
}