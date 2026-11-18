namespace Asset.Application.Features.AI.DTos
{
    // Everything the answer writer needs to write one reply.
    // The handler fills it AFTER the permission checks, so the writer can never see
    // more than the user is allowed to see.
    public class AssetAnswerContext
    {
        public string Question { get; init; } = string.Empty;

        // The earlier turns of this chat, oldest first.
        public IReadOnlyList<ChatTurn> History { get; init; } = Array.Empty<ChatTurn>();

        // The answer written by code (AssetAnswerBuilder). Always correct.
        // The AI may rephrase it but never contradict it, and it is the reply used if the AI fails.
        public string Facts { get; init; } = string.Empty;

        // The rows shown in the table (PurchaseCost is already null for non-admins).
        public IReadOnlyList<AssetQuestionResultDto> Assets { get; init; } = Array.Empty<AssetQuestionResultDto>();

        // How many assets match in the database (can be more than Assets.Count, which is capped).
        public int TotalCount { get; init; }
    }
}
