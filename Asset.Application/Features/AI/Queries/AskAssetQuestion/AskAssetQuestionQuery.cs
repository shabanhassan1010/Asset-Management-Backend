using Asset.Application.Common.Responses;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQuery : IRequest<ApiResponse<AssetQuestionResponse>>
    {
        public string Question { get; set; } = string.Empty;
    }
}
