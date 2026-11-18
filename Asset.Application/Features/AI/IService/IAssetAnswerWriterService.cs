using Asset.Application.Features.AI.DTos;

namespace Asset.Application.Features.AI.IService
{
    // Turns the facts and rows into the sentence the user reads.
    // Two implementations:
    //  - GeminiAssetAnswerWriter (Infrastructure): a generated, natural reply.
    //  - TemplateAssetAnswerWriter (Application): returns the template text as it is
    //    (used when there is no API key, or when Gemini:GenerateAnswers is false).
    public interface IAssetAnswerWriterService
    {
        // Never throws for an AI problem: on any failure it returns context.Facts.
        Task<string> WriteAnswerAsync(AssetAnswerContext context, CancellationToken cancellationToken);
    }
}
