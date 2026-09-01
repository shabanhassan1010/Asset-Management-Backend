using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQuery : IRequest<ApiResponse<AssetQuestionResponse>>
    {
        public string Question { get; set; } = string.Empty;
    }
}