#region
using Asset.Application.Common.Interfaces;
using Asset.Application.Common.Models;
using Asset.Application.Features.Users.DTOs;
using Asset.Application.Interfaces.Comman;
using Asset.Application.Interfaces.IRepository;
using Asset.Domain.Exceptions;
using Asset.Domain.Identity;
using Asset.Domain.Models;
using AutoMapper;
using MediatR;
#endregion

namespace Asset.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserListItemDto>
{
    #region Fields
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _users;
    private readonly IMapper _mapper;
    #endregion

    #region Constructor
    public CreateUserCommandHandler(IUnitOfWork unitOfWork, IUserRepository users, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _users = users;
        _mapper = mapper;
    }
    #endregion

    #region Handlers
    public async Task<UserListItemDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        if (await _users.UserNameExistsAsync(request.UserName, cancellationToken))
            throw new ConflictException($"Username '{request.UserName}' is already taken.");

        if (await _users.EmailExistsAsync(request.Email, cancellationToken))
            throw new ConflictException($"Email '{request.Email}' is already in use.");

        if (!await _unitOfWork.Employees.ExistsAsync(request.EmployeeId, cancellationToken))
            throw new NotFoundException($"Employee {request.EmployeeId} was not found.");

        if (await _users.EmployeeHasUserAsync(request.EmployeeId, cancellationToken))
            throw new ConflictException($"Employee {request.EmployeeId} already has a user account.");

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            EmployeeId = request.EmployeeId
        };

        var errors = await _users.CreateAsync(user, request.Password, request.Role, cancellationToken);
        if (errors.Count > 0)
            throw new ConflictException(string.Join(" ", errors));

        return _mapper.Map<UserListItemDto>(new UserWithRole(user, request.Role));
    }
    #endregion
}