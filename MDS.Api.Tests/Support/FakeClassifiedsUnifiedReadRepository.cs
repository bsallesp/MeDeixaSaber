using MDS.Application.Abstractions.Data;

namespace MDS.Api.Tests.Support;

public sealed class FakeClassifiedsUnifiedReadRepository : IClassifiedsUnifiedReadRepository
{
    private readonly List<ClassifiedUnifiedDto> _data;

    public FakeClassifiedsUnifiedReadRepository()
    {
        var todayIso = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

        _data = Enumerable.Range(1, 20)
            .Select(i => new ClassifiedUnifiedDto(
                $"Item {i}",
                todayIso,
                $"Desc {i}",
                Array.Empty<string>(),
                $"https://example.com/{i}"
            ))
            .ToList();
    }

    public Task<IReadOnlyList<ClassifiedUnifiedDto>> GetTopAsync(int take, int skip, CancellationToken ct = default)
    {
        var res = _data.Skip(skip).Take(take).ToList();
        return Task.FromResult<IReadOnlyList<ClassifiedUnifiedDto>>(res);
    }

    public Task<IReadOnlyList<ClassifiedUnifiedDto>> GetByDayAsync(DateTime day, int take, int skip, CancellationToken ct = default)
    {
        var dayIso = day.ToString("yyyy-MM-dd");
        var res = _data
            .Where(x => string.Equals(x.PostDate, dayIso, StringComparison.Ordinal))
            .Skip(skip)
            .Take(take)
            .ToList();
        return Task.FromResult<IReadOnlyList<ClassifiedUnifiedDto>>(res);
    }
}