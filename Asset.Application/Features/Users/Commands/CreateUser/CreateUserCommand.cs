using Asset.Application.Features.Users.DTOs;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using MediatR;

namespace Asset.Application.Features.Users.Commands.CreateUser;
public class CreateUserCommand : IRequest<UserListItemDto>
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int EmployeeId { get; set; }
}
