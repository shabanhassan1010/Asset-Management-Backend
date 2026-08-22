#region
using Asset.API.Helper;
using Asset.Application.Features.Category.Commands.CommandModels;
using Asset.Application.Features.Category.Queries.QueryModels;
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
    public class CategoryController : ControllerBase
    {
        #region Fields
        private readonly ISender _sender;
        #endregion

        #region Constructor
        public CategoryController(ISender sender)
        {
            _sender = sender;
        }
        #endregion

        #region GetList
        [HttpGet(BaseRouter.CategoryRouter.Base)]
        public async Task<IActionResult> GetList(CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetCategoryListQueryModel(), ct));
        }
        #endregion

        #region GetById
        [HttpGet(BaseRouter.CategoryRouter.Id)]
        public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
        {
            return Ok(await _sender.Send(new GetCategoryByIdQueryModel(id), ct));
        }
        #endregion

        #region Create
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.CategoryRouter.Base)]
        public async Task<IActionResult> Create([FromBody] CreateCategoryCommandModel command, CancellationToken ct)
        {
            var result = await _sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.data.Id }, result);
        }
        #endregion

        #region Update
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPut(BaseRouter.CategoryRouter.Id)]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCategoryCommandModel command, CancellationToken ct)
        {
            command.Id = id;
            var result = await _sender.Send(command, ct);
            return NoContent();
        }
        #endregion

        #region Delete
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpDelete(BaseRouter.CategoryRouter.Id)]
        public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
        {
            var result = await _sender.Send(new DeleteCategoryCommandModel(id), ct);
            return NoContent();
        }
        #endregion
    }
}
