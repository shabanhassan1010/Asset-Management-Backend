namespace Asset.Application.Common.Responses
{
    public class ApiResponse<T> : IHasMessage
    {
        public bool Success { get; set; }
        // Handlers write the English text; LocalizationBehavior translates it to the request's language.
        public string Message { get; set; }
        public T data { get; set; }
    }
}
