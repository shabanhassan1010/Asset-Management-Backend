#region
using Asset.API.Helper;
using Asset.Application.Features.Dashboard.DTos;
using Asset.Application.Features.Dashboard.Queries.QueryHandlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
#endregion

namespace Asset.API.Controllers
{
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        #region Fields
        private readonly ISender sender;
        #endregion

        #region Constructor
        public DashboardController(ISender sender)
        {
            this.sender = sender;
        }
        #endregion

        #region Summary
        [HttpGet(BaseRouter.DashboardRouter.Summary)]
        [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
        {
            var summary = await sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
            return Ok(summary);
        }
        #endregion
    }
}