using Asset.Application.Features.AI.Enums.DTos;
namespace Asset.Application.Features.AI.Interfases
{
    public interface IAssetQuestionParser
    {
        // change and every caller would have to be rewritten - which defeats the point of having the interface at all.
        Task<ParsedAssetQuestion> ParseAsync(string question, CancellationToken cancellationToken);
    }
}
