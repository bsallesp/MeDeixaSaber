using System.Data.Common;

namespace MDS.Data.Context;

public interface IDbConnectionFactory
{
    Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default);
}