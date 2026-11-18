using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.IService;

namespace Asset.Application.Features.AI.ServiceImplementation
{
    // The "no AI" writer: returns the template answer from AssetAnswerBuilder unchanged.
    // Used when there is no Gemini key, or when Gemini:GenerateAnswers is false
    // (for example when asset data must not be sent to an external AI).
    public class TemplateAssetAnswerWriter : IAssetAnswerWriterService
    {
        public Task<string> WriteAnswerAsync(AssetAnswerContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(context.Facts);
        }
    }
}
