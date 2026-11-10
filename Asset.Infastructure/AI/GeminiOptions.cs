namespace Asset.Infastructure.AI
{
    // Bound to the "Gemini" section of the configuration.
    // ApiKey comes from user-secrets, Model comes from appsettings.json.
    public class GeminiOptions
    {
        public const string SectionName = "Gemini";

        public string ApiKey { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
    }
}