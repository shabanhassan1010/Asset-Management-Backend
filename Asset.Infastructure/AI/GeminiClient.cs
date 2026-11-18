#region
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
#endregion

namespace Asset.Infastructure.AI
{
    // One message of the conversation sent to Gemini. Role is "user" or "model" (Gemini's word for the AI).
    public record GeminiMessage(string Role, string Text);

    // The only class that talks HTTP to Gemini. The parser and the answer writer both use it,
    // so the request format, the API key header and the error logging are written once.
    public class GeminiClient
    {
        #region Fields
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiClient> _logger;
        #endregion

        #region Constructor
        public GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region Methods
        // instructions = the rules (system instruction), messages = the conversation, oldest first.
        // jsonOutput   = true when we need JSON back (the parser), false for normal text (the writer).
        // temperature  = 0 for the same answer every time, higher for more natural wording.
        // Throws on any problem; the caller catches it and falls back.
        public async Task<string> GenerateAsync(string instructions,
                                                IReadOnlyList<GeminiMessage> messages,
                                                bool jsonOutput,
                                                double temperature,
                                                CancellationToken cancellationToken)
        {
            var body = new
            {
                systemInstruction = new { parts = new[] { new { text = instructions } } },
                contents = messages.Select(m => new { role = m.Role, parts = new[] { new { text = m.Text } } }),
                generationConfig = new
                {
                    temperature,
                    responseMimeType = jsonOutput ? "application/json" : "text/plain"
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, $"v1beta/models/{_options.Model}:generateContent");
            request.Headers.Add("x-goog-api-key", _options.ApiKey);   // header, not URL, so the key never shows in logs
            request.Content = JsonContent.Create(body);

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Google's error body says WHY (which quota, wrong model...). It never contains the key.
                var error = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    _logger.LogWarning("Gemini quota reached (429): {Error}", error);
                else
                    _logger.LogWarning("Gemini returned {StatusCode}: {Error}", (int)response.StatusCode, error);

                response.EnsureSuccessStatusCode();   // throws -> the caller falls back
            }

            using var result = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken),
                                                             cancellationToken: cancellationToken);

            if (result.RootElement.TryGetProperty("usageMetadata", out var usage)
                && usage.TryGetProperty("totalTokenCount", out var tokens))
            {
                _logger.LogInformation("Gemini call used {Tokens} tokens", tokens.GetInt32());
            }

            // Gemini wraps the text like this: candidates[0].content.parts[0].text
            // A blocked or empty answer has no candidates: that becomes an exception -> fallback.
            if (!result.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                throw new InvalidOperationException("Gemini returned no answer.");

            var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            if (string.IsNullOrWhiteSpace(text))
                throw new InvalidOperationException("Gemini returned an empty answer.");

            return text;
        }
        #endregion
    }
}
