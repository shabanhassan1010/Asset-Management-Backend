#region
using Asset.API.Helper;
using Asset.Application.Common.Responses;
using Asset.Application.Features.Users.Commands.ChangeUserRole;
using Asset.Application.Features.Users.Commands.ChangeUserStatus;
using Asset.Application.Features.Users.Commands.CreateUser;
using Asset.Application.Features.Users.DTOs;
using Asset.Application.Features.Users.Queries.GetUsers;
using Asset.Domain.Enum;
using Asset.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Asset.API.Controllers;
#endregion

[ApiController]
[Authorize(Roles = nameof(Role.Admin))]
public class UsersController : ControllerBase
{
    #region Fields
    private readonly ISender _sender;
    #endregion

    #region Constructor
    public UsersController(ISender sender)
    {
        _sender = sender;
    }
    #endregion

    #region GetUsers
    [HttpGet(BaseRouter.UserRouter.Base)]
    [ProducesResponseType(typeof(PagedResult<UserListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserListItemDto>>> GetUsers([FromQuery] GetUsersQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(query, cancellationToken));
    }
    #endregion

    #region CreateUser
    [HttpPost(BaseRouter.UserRouter.Base)]
    [ProducesResponseType(typeof(UserListItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserListItemDto>> CreateUser(CreateUserCommand command,CancellationToken cancellationToken)
    {
        var created = await _sender.Send(command, cancellationToken);
        return Created($"/api/users?search={created.UserName}", created);
    }
    #endregion

    #region ChangeRole
    [HttpPut(BaseRouter.UserRouter.Role)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeRole(string id,ChangeUserRoleRequest request,CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeUserRoleCommand(id, request.Role), cancellationToken);
        return NoContent();
    }
    #endregion

    #region ChangeStatus
    [HttpPut(BaseRouter.UserRouter.Status)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(string id,ChangeUserStatusRequest request,CancellationToken cancellationToken)
    {
        await _sender.Send(new ChangeUserStatusCommand(id, request.IsActive), cancellationToken);
        return NoContent();
    }
    #endregion
}

public record ChangeUserRoleRequest(Role Role);
public record ChangeUserStatusRequest(bool IsActive);