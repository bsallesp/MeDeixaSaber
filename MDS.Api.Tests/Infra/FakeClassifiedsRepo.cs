using MDS.Application.Abstractions.Data;

namespace MDS.Api.Tests.Infra;

public sealed class FakeClassifiedsRepo : IClassifiedsUnifiedReadRepository
{
    readonly List<ClassifiedUnifiedDto> _data;

    public FakeClassifiedsRepo()
    {
        var todayIso = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");
        _data = Enumerable.Range(1, 10)
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
        => Task.FromResult<IReadOnlyList<ClassifiedUnifiedDto>>(_data.Skip(skip).Take(take).ToList());

    public Task<IReadOnlyList<ClassifiedUnifiedDto>> GetByDayAsync(DateTime day, int take, int skip, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<ClassifiedUnifiedDto>>(_data.Skip(skip).Take(take).ToList());
}