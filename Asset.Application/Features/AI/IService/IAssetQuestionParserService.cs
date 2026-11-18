using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.Interfases
{
    public interface IAssetQuestionParserService
    {
        // history  = the whole chat so far, so "them", "his", "the first one" can be understood.
        // previous = the exact filters of the last search (a precise hint the chat text doesn't carry).
        Task<ParsedAssetQuestion> ParseAsync(string question,
                                             IReadOnlyList<ChatTurn> history,
                                             ParsedAssetQuestion? previous,
                                             CancellationToken cancellationToken);
    }
}
