using Asset.Application.Features.AI.Enums.DTos;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AssetQuestionResponse
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public IReadOnlyList<AssetQuestionResultDto> Assets { get; set; }= Array.Empty<AssetQuestionResultDto>();
        public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
        public int TotalCount { get; set; }
    }
}