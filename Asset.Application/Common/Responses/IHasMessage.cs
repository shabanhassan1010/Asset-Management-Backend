namespace Asset.Application.Common.Responses
{
    // Implemented by every response type that has a user-facing Message
    // (ApiResponse<T> and BaseResponse<T>), so LocalizationBehavior can translate it
    // without knowing which response type it is.
    public interface IHasMessage
    {
        string Message { get; set; }
    }
}
