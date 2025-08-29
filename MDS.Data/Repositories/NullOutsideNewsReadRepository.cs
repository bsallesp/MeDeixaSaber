using MDS.Application.Abstractions.Data;
using MeDeixaSaber.Core.Models;

namespace MDS.Data.Repositories;

public sealed class NullOutsideNewsReadRepository : IOutsideNewsReadRepository
{
    public Task<IReadOnlyList<OutsideNews>> GetTopAsync(int pageSize, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<OutsideNews>>([]);
}
