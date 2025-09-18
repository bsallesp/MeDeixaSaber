using MeDeixaSaber.Core.Services;

namespace MDS.Data.Tests.Repositories;

public sealed class FakeNormalizer : ITitleNormalizationService
{
    public string Normalize(string? title)
    {
        return title ?? string.Empty;
    }
}