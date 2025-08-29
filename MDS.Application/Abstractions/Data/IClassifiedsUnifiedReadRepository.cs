using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MDS.Application.Abstractions.Data;

public interface IClassifiedsUnifiedReadRepository
{
    Task<IReadOnlyList<ClassifiedUnifiedDto>> GetTopAsync(int take, int skip, CancellationToken ct = default);
    Task<IReadOnlyList<ClassifiedUnifiedDto>> GetByDayAsync(DateTime dayUtc, int take, int skip, CancellationToken ct = default);
}