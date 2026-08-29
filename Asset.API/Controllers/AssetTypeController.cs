#region
using Asset.API.Helper;
using Asset.Application.Features.AssetTypes.Commands.CommandModels;
using Asset.Application.Features.AssetTypes.Queries.QueryModels;
using Asset.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class AssetTypeController(ISender sender) : ControllerBase
    {
        #region GetList
        [HttpGet(BaseRouter.AssetTypeRouter.Base)]
        public async Task<IActionResult> GetList(CancellationToken cancellationToken)
        {
            return Ok(await sender.Send(new GetAssetTypeListQueryModel(), cancellationToken));
        }
        #endregion

        #region GetById
        [HttpGet(BaseRouter.AssetTypeRouter.Id)]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new GetAssetTypeByIdQueryModel { Id = id }, cancellationToken);
            return Ok(response);
        }
        #endregion

        #region Create
        [HttpPost(BaseRouter.AssetTypeRouter.Base)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> Create([FromBody] CreateAssetTypeCommandModel command, CancellationToken cancellationToken)
        {
            var response = await sender.Send(command, cancellationToken);
            return Ok(response);
        }
        #endregion

        #region Update
        [HttpPut(BaseRouter.AssetTypeRouter.Id)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateAssetTypeCommandModel command, CancellationToken cancellationToken)
        {
            command.Id = id;
            var response = await sender.Send(command, cancellationToken);
            return Ok(response);
        }
        #endregion

        #region Delete
        [HttpDelete(BaseRouter.AssetTypeRouter.Id)]
        [Authorize(Roles = nameof(Role.Admin))]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var response = await sender.Send(new DeleteAssetTypeCommandModel { Id = id }, cancellationToken);
            return Ok(response);
        }
        #endregion
    }
}
