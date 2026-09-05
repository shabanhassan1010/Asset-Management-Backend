using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.Interfases
{
    public interface IAssetQuestionParserService
    {
        // change and every caller would have to be rewritten - which defeats the point of having the interface at all.
        Task<ParsedAssetQuestion> ParseAsync(string question, ParsedAssetQuestion? previous , CancellationToken cancellationToken);
    }
}
