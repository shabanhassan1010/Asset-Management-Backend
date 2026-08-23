#region
using Asset.API.Helper;
using Asset.Application.Features.Auth.Commands.Login;
using Asset.Application.Features.Auth.Commands.Logout;
using Asset.Application.Features.Auth.Commands.Refresh;
using Asset.Application.Features.Auth.DTOs;
using Asset.Application.Features.Auth.Queries.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Asset.API.Controllers;
#endregion

[ApiController]
[Authorize]
public class AuthController : ControllerBase
{

    #region Fields
    private readonly ISender _sender;
    #endregion

    #region Constructor
    public AuthController(ISender sender)
    {
        _sender = sender;
    }
    #endregion

    #region Login
    [HttpPost(BaseRouter.AuthRouter.Login)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Login( LoginCommand command,CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(command, cancellationToken));
    }
    #endregion

    #region Refresh
    [HttpPost(BaseRouter.AuthRouter.Refresh)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponseDto>> Refresh(RefreshTokenCommand command,CancellationToken cancellationToken)
    {
        return Ok(await _sender.Send(command, cancellationToken));
    }
    #endregion

    #region Logout
    [HttpPost(BaseRouter.AuthRouter.Logout)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutCommand command, CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
    #endregion

    #region Me
    [HttpGet(BaseRouter.AuthRouter.Me)]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        return  Ok(await _sender.Send(new GetCurrentUserQuery(), cancellationToken));
    }
    #endregion
}