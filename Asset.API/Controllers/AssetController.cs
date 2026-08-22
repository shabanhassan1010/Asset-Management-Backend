#region
using Asset.API.Helper;
using Asset.Application.Features.Assets.Commands.CommandModels;
using Asset.Application.Features.Assets.Queries.QueryModels;
using Asset.Application.Features.AssetTransfers.CommandModel;
using Asset.Application.Features.GetAssetTransferHistory.QueryModels;
using Asset.Domain.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class AssetController : ControllerBase
    {
        #region Fields
        private readonly ISender _Sender;
        #endregion

        #region Constructor
        public AssetController(ISender mediator)
        {
            _Sender = mediator;
        }
        #endregion

        #region Get By Id
        [HttpGet(BaseRouter.AssetRouter.Id)]
        public async Task<IActionResult> GetById([FromRoute]int id, CancellationToken ct)
        {
            return Ok(await _Sender.Send(new GetAssetByIdQueryModel(id), ct));
        }
        #endregion

        #region Retire
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.AssetRouter.Retire)]
        public async Task<IActionResult> Retire([FromRoute] int id, [FromBody] RetireAssetCommandModel command, CancellationToken ct)
        {
            command.AssetId = id;
            return Ok(await _Sender.Send(command, ct));
        }
        #endregion

        #region
        //[HttpGet(BaseRouter.AssetRouter.Base)] 
        //public async Task<IActionResult> GetAll(CancellationToken cancellationToken) 
        //{ 
        //    var query = new GetAssetListQueryModel(); 
        //    var result = await _mediator.Send(query, cancellationToken); 
        //    return Ok(result); 
        //}
        #endregion

        #region Paginated
        [HttpGet(BaseRouter.AssetRouter.Paginated)] 
        public async Task<IActionResult> GetPaginated([FromQuery] GetAssetPaginatedListQueryModel request, CancellationToken cancellationToken) 
        { 
            var result = await _Sender.Send(request, cancellationToken); 
            return Ok(result); 
        }
        #endregion

        #region Create
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.AssetRouter.Base)]
        public async Task<IActionResult> Create([FromBody] CreateAssetCommandModel command, CancellationToken ct)
        {
            var result = await _Sender.Send(command, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.data.AssetId }, result);
        }
        #endregion

        #region Update
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPut(BaseRouter.AssetRouter.Id)]
        public async Task<IActionResult> Update([FromRoute] int id,[FromBody] UpdateAssetCommandModel command,CancellationToken ct)
        {
            command.AssetId = id;
            var result = await _Sender.Send(command, ct);
            return NoContent();
        }
        #endregion

        #region GetTransferHistory
        [HttpGet(BaseRouter.AssetRouter.Transfers)]
        public async Task<IActionResult> GetTransferHistory([FromRoute] int id, CancellationToken ct)
        {
            return Ok(await _Sender.Send(new GetAssetTransferHistoryQueryModel(id), ct));
        }
        #endregion

        #region Transfer
        [Authorize(Roles = nameof(Role.Admin))]
        [HttpPost(BaseRouter.AssetRouter.Transfers)]
        public async Task<IActionResult> Transfer([FromRoute] int id, [FromBody] TransferAssetCommandModel command,CancellationToken ct)
        {
            command.AssetId = id;  
            return Ok(await _Sender.Send(command, ct));
        }
        #endregion
    }
}