#region
using Asset.API.Helper;
using Asset.Application.Features.Departments.Commands.CommandModels;
using Asset.Application.Features.Departments.Queries.QueryModels;
using Asset.Application.Features.Locations.Commands.CommandModels;
using Asset.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class DepartmentController : ControllerBase
    {
        #region Fields
        private readonly ISender _sender;
        #endregion

        #region Constructor
        public DepartmentController(ISender sender)
        {
            _sender = sender;
        }
        #endregion

        #region GetList
        [HttpGet(BaseRouter.DepartmentRouter.Base)]
        public async Task<IActionResult> GetList(CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetDepartmentListQueryModel(), ct));
        }
        #endregion

        #region GetById
        [HttpGet(BaseRouter.DepartmentRouter.Id)]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetDepartmentByIdQueryModel(id), ct));
        }
        #endregion

        #region Create
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.DepartmentRouter.Base)]
        public async Task<IActionResult> Create([FromBody] CreateDepartmentCommandModel command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.data.Id }, result);
        }
        #endregion

        #region Update
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPut(BaseRouter.DepartmentRouter.Id)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateDepartmentCommandModel command, CancellationToken ct)
        {
            command.Id = id;
            var result = await _sender.Send(command, ct);
            return NoContent();
        }
        #endregion

        #region Delete
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpDelete(BaseRouter.DepartmentRouter.Id)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            var result = await _sender.Send(new DeleteDepartmentCommandModel(id), ct);
            return NoContent();
        }
        #endregion
    }
}