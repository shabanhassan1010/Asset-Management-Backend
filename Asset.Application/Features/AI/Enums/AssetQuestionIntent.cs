namespace Asset.Application.Features.AI.Enums
{
    public enum AssetQuestionIntent
    {
        ListAssets,
        CountAssets,

        Greeting,
        // Every question the parser cannot map to a read falls here.
        // It is a real value (not null) so the handler is forced to deal with it.
        Unsupported
    }
}
