using System.Text.Json;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Collectors;

namespace MDS.Runner.NewsLlm.Application
{
    public sealed class GoogleImageFinder : IImageFinder
    {
        private readonly HttpClient _http;
        private readonly ISecretReader _secretReader;
        private readonly string _cxName;

        public GoogleImageFinder(HttpClient http, ISecretReader secretReader, string cxName = "google-search-cx-id")
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _secretReader = secretReader ?? throw new ArgumentNullException(nameof(secretReader));
            _cxName = cxName;
        }

        public async Task<string?> FindImageUrlAsync(string title, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

            string apiKey;
            string cxId;
            try
            {
                apiKey = await _secretReader.GetAsync("google-search-api-key", ct);
                cxId = await _secretReader.GetAsync(_cxName, ct);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAGE FINDER ERROR] Falha ao ler secrets do Key Vault: {ex.Message}");
                return null;
            }


            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(cxId))
            {
                Console.WriteLine("[IMAGE FINDER] API Key ou CX ID ausente ou vazio.");
                return null;
            }

            var encodedQuery = Uri.EscapeDataString(title);
            var url = $"https://www.googleapis.com/customsearch/v1?key={apiKey}&cx={cxId}&q={encodedQuery}&searchType=image&num=1";

            try
            {
                using var response = await _http.GetAsync(url, ct);
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[IMAGE FINDER ERROR] Status {response.StatusCode} na busca de imagem. Body: {await response.Content.ReadAsStringAsync(ct)}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                
                var firstItem = doc.RootElement.TryGetProperty("items", out var items) && items.GetArrayLength() > 0 ? items[0] : default;
                
                if (firstItem.TryGetProperty("link", out var link) && link.ValueKind == JsonValueKind.String)
                {
                    var imageUrl = link.GetString();
                    Console.WriteLine($"[IMAGE FINDER] Imagem encontrada para '{title}': {imageUrl}");
                    return imageUrl;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IMAGE FINDER ERROR] Falha na busca de imagem para '{title}': {ex.Message}");
            }

            return null;
        }
    }
}