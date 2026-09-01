using Asset.Application.Features.AI.Enums.DTos;
namespace Asset.Application.Features.AI.Queries.AskAssetQuestion
{
    public class AssetQuestionResponse
    {
        // Echoed back so the frontend can pair an answer with its question
        // even if two requests come back out of order.
        public string Question { get; set; } = string.Empty;

        // Built on the server from the real result, never by the parser.
        // That is what guarantees the number in the sentence always matches
        // the number of rows in the table.
        public string Answer { get; set; } = string.Empty;

        public IReadOnlyList<AssetQuestionResultDto> Assets { get; set; }= Array.Empty<AssetQuestionResultDto>();
        public IReadOnlyList<string> Suggestions { get; set; } = Array.Empty<string>();
        // Total matches in the database, which can be larger than Assets.Count
        // because the list is capped. The UI uses this to say "showing 20 of 63".
        public int TotalCount { get; set; }
    }
}