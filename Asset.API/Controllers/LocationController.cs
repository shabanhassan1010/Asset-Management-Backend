#region
using Asset.API.Helper;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Application.Features.Locations.Queries.QueryModels;
using Asset.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class LocationController : ControllerBase
    {
        #region Fields
        private readonly ISender _sender;
        #endregion

        #region Constructor
        public LocationController(ISender sender)
        {
            _sender = sender;
        }
        #endregion

        #region GetList
        [HttpGet(BaseRouter.LocationRouter.Base)]
        public async Task<IActionResult> GetList(CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetLocationListQueryModel (), ct));
        }
        #endregion

        #region GetById
        [HttpGet(BaseRouter.LocationRouter.Id)]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetLocationByIdQueryModel(id), ct));
        }
        #endregion

        #region Create
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.LocationRouter.Base)]
        public async Task<IActionResult> Create([FromBody] CreateLocationCommandModel command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById) , new { id = result.data.Id} , result);
        }
        #endregion

        #region Update
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPut(BaseRouter.LocationRouter.Id)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateLocationCommandModel command, CancellationToken ct)
        {
            command.Id = id;
            var result = await _sender.Send(command, ct);
            return NoContent();
        }
        #endregion

        #region Delete
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpDelete(BaseRouter.LocationRouter.Id)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            var result = await _sender.Send(new DeleteLocationCommandModel(id), ct);
            return NoContent();
        }
        #endregion
    }
}