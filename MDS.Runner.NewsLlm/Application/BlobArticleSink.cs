using System.Text.Json;
using MDS.Runner.NewsLlm.Abstractions;
using MDS.Runner.NewsLlm.Persisters;
using MeDeixaSaber.Core.Models;

namespace MDS.Runner.NewsLlm.Application;

public sealed class BlobArticleSink(IBlobSaver saver, string prefix = "news-llm") : IArticleSink
{
    private readonly IBlobSaver _saver = saver ?? throw new ArgumentNullException(nameof(saver));
    private readonly string _prefix = string.IsNullOrWhiteSpace(prefix) ? "news-llm" : prefix;

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task InsertAsync(OutsideNews item)
    {
        if (item.CreatedAt == default) item.CreatedAt = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(item, JsonOpts);
        await _saver.SaveJsonAsync(json, _prefix);
    }
}