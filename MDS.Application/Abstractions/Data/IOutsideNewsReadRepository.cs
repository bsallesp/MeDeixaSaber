using MeDeixaSaber.Core.Models;

namespace MDS.Application.Abstractions.Data;

public interface IOutsideNewsReadRepository
{
    Task<IReadOnlyList<OutsideNews>> GetTopAsync(int pageSize, CancellationToken ct = default);
    
    Task<OutsideNews?> GetByIdAsync(string id, CancellationToken ct = default);
}