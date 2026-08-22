#region
using Asset.API.Helper;
using Asset.Application.Features.AssetTypes.Queries.QueryModels;
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
    }
}
