using System.Text.Json;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Collectors;

namespace MDS.Runner.NewsLlm.Application
{
    public sealed class UnsplashImageFinder : IImageFinder
    {
        private readonly HttpClient _http;
        private readonly ISecretReader _secretReader;
        private readonly Random _random = new();
        private const string ApiKeyName = "unsplash-api-key";
        private const int MaxResultsForRandomSelection = 5; // Busca as 5 melhores imagens

        public UnsplashImageFinder(HttpClient http, ISecretReader secretReader)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _secretReader = secretReader ?? throw new ArgumentNullException(nameof(secretReader));
            _http.BaseAddress = new Uri("https://api.unsplash.com/");
        }

        public async Task<string?> FindImageUrlAsync(string title, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            string apiKey;
            try
            {
                apiKey = await _secretReader.GetAsync(ApiKeyName, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAGE FINDER ERROR] Falha ao ler secret {ApiKeyName} do Key Vault: {ex.Message}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine($"[IMAGE FINDER] {ApiKeyName} ausente ou vazio.");
                return null;
            }

            var encodedQuery = Uri.EscapeDataString(title);
            // Solicita um número maior de resultados para permitir a seleção aleatória
            var url = $"search/photos?query={encodedQuery}&orientation=landscape&per_page={MaxResultsForRandomSelection}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Client-ID {apiKey}");

            try
            {
                using var response = await _http.SendAsync(request, ct);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[IMAGE FINDER ERROR] Unsplash API Status {response.StatusCode}.");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                
                if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                {
                    Console.WriteLine($"[IMAGE FINDER] Nenhuma imagem encontrada para '{title}'.");
                    return null;
                }
                
                var resultsCount = results.GetArrayLength();
                // Gera um índice aleatório entre 0 e resultsCount - 1
                var randomIndex = _random.Next(0, resultsCount);
                
                var randomResult = results[randomIndex];
                
                if (randomResult.TryGetProperty("urls", out var urls) && 
                    urls.TryGetProperty("regular", out var regularUrl) && 
                    regularUrl.ValueKind == JsonValueKind.String)
                {
                    var imageUrl = regularUrl.GetString();
                    Console.WriteLine($"[IMAGE FINDER] Imagem (Unsplash) selecionada aleatoriamente (Index: {randomIndex}) para '{title}': {imageUrl}");
                    return imageUrl;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAGE FINDER ERROR] Falha na busca de imagem Unsplash para '{title}': {ex.Message}");
            }

            return null;
        }
    }
}