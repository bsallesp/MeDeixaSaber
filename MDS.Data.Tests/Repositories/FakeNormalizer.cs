using MeDeixaSaber.Core.Services;

namespace MDS.Data.Tests.Repositories;

public sealed class FakeNormalizer(string suffix) : ITitleNormalizationService
{
    readonly string _suffix = suffix;
    public string Normalize(string title) => title + _suffix;
}