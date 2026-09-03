using Asset.Application.Common.Responses;
using MediatR;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AskAssetQuestionQuery : IRequest<ApiResponse<AssetQuestionResponse>>
    {   
        public string Question { get; set; } = string.Empty;  // use (string.Empty) to avoid null reference issues if user does not provide a question.
    }
}