#region
using Asset.API.Helper;
using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Queries.AskAssetQuestion;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class AIController : ControllerBase
    {
        #region Fields
        private readonly ISender _sender;
        #endregion

        #region Constructor
        public AIController(ISender sender)
        {
            _sender = sender;
        }
        #endregion

        #region  Ask AI
        [HttpPost(BaseRouter.AIRouter.Ask)]
        [ProducesResponseType(typeof(ApiResponse<AssetQuestionResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> Ask([FromBody] AskAssetQuestionQuery query, CancellationToken cancellationToken)
        {
            var result = await _sender.Send(query, cancellationToken);

            return Ok(result);
        }
        #endregion
    }
}