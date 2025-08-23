using System.Data.Common;
using MDS.Data.Context;

namespace MDS.Data.Tests.Repositories;

public sealed class FakeFactory(Func<DbConnection> create) : IDbConnectionFactory
{
    readonly Func<DbConnection> _create = create;
    public Task<DbConnection> GetOpenConnectionAsync(CancellationToken ct = default) => Task.FromResult(_create());
}