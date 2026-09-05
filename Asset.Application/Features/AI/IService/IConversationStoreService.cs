using Asset.Application.Features.AI.Enums.DTos;

namespace Asset.Application.Features.AI.IService
{
    public interface IConversationStoreService
    {
        ParsedAssetQuestion? GetLast(string sessionId);
        void SaveLast(string sessionId, ParsedAssetQuestion parsed);
    }
}