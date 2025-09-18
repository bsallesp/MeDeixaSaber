using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingConnection(Exception exception) : DbConnection
{
    public override string? ConnectionString { get; set; }
    public override string Database => "db";
    public override string DataSource => "server";
    public override string ServerVersion => "1";
    public override ConnectionState State => ConnectionState.Closed;

    public override void Open() => throw exception;

    public override Task OpenAsync(CancellationToken cancellationToken) => Task.FromException(exception);

    public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();

    public override void Close()
    {
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotImplementedException();

    protected override DbCommand CreateDbCommand() => new ThrowingCommand(exception);
}