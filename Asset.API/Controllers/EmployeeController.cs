#region
using Asset.API.Helper;
using Asset.Application.Features.Employees.Commands.CommandModels;
using Asset.Application.Features.Employees.Queries.QueryModels;
using Asset.Application.Features.Employees.Queries.QueryResponses;
using Asset.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        #region Fields
        private readonly ISender _sender;
        #endregion

        #region Constructor
        public EmployeeController(ISender sender)
        {
            _sender = sender;
        }
        #endregion

        #region Paginated
        [HttpGet(BaseRouter.EmployeeRouter.Paginated)]
        public async Task<IActionResult> GetEmployees([FromQuery] GetEmployeesPaginatedQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);
            return Ok(result);
        }
        #endregion

        #region GetEmployeeById
        [HttpGet(BaseRouter.EmployeeRouter.Id)]
        public async Task<IActionResult> GetEmployeeById(int id, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(new GetEmployeeByIdQueryModel(id), cancellationToken);
            return Ok(result);
        }
        #endregion

        #region GetAvailableEmployees
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpGet(BaseRouter.EmployeeRouter.AvailableForUser)]
        [ProducesResponseType(typeof(IReadOnlyList<AvailableEmployeeDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<AvailableEmployeeDto>>> GetAvailableEmployees([FromQuery] int? departmentId, CancellationToken cancellationToken)
        {
            return Ok(await _sender.Send(new GetAvailableEmployeesQueryModel(departmentId), cancellationToken));
        }
        #endregion

        #region CreateEmployee
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.EmployeeRouter.Base)]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommandModel command, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetEmployeeById), new { id = result.data.Id }, result);
        }
        #endregion

        #region UpdateEmployee
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPut(BaseRouter.EmployeeRouter.Id)]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] UpdateEmployeeCommandModel command, CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _sender.Send(command, cancellationToken);
            return NoContent();
        }
        #endregion

        #region SetEmployeeStatus
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPatch(BaseRouter.EmployeeRouter.Status)]
        public async Task<IActionResult> SetEmployeeStatus(int id, [FromBody] SetEmployeeStatusCommandModel command, CancellationToken cancellationToken)
        {
            command.Id = id;
            var result = await _sender.Send(command, cancellationToken);
            return Ok(result);
        }
        #endregion
    }
}