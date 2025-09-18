using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using MDS.Data.Context;

namespace MDS.Data.Tests.Repositories;

public sealed class FakeFactory(DbConnection connection) : IDbConnectionFactory
{
    public Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default)
    {
        return Task.FromResult(connection);
    }
}