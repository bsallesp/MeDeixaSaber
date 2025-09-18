using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MDS.Runner.NewsLlm.Integrations.Gemini.Dto;

namespace MDS.Runner.NewsLlm.Integrations.Gemini
{
    public sealed class GeminiService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly string _model;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public GeminiService(HttpClient http, string apiKey, string model = "gemini-1.5-flash")
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public async Task<GeminiResponseDto?> GenerateContentAsync(string prompt, bool useSearch, CancellationToken ct = default)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var payload = new GeminiRequestDto
            {
                Contents =
                [
                    new ContentDto
                    {
                        Parts = [new PartDto { Text = prompt }]
                    }
                ],
                Tools = useSearch ? [new ToolDto()] : null
            };

            var jsonPayload = JsonSerializer.Serialize(payload, JsonOptions);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            var response = await _http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[GEMINI ERROR] Status: {response.StatusCode}, Body: {errorBody}");
                return null;
            }

            var responseStream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonSerializer.DeserializeAsync<GeminiResponseDto>(responseStream, JsonOptions, ct);
        }
    }
}