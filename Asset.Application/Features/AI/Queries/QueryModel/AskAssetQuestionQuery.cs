using Asset.Application.Common.Responses;
using Asset.Application.Features.AI.Queries.QueryResponse;
using MediatR;
namespace Asset.Application.Features.AI.Queries.QueryModel
{
    public class AskAssetQuestionQuery : IRequest<ApiResponse<AssetQuestionResponse>>
    {   
        public string Question { get; set; } = string.Empty;  // use (string.Empty) to avoid null reference issues if user does not provide a question.
        public string SessionId { get; set; } = string.Empty;
    }
}