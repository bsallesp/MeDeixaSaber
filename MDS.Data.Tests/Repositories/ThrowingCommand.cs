using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingCommand(Exception exception) : DbCommand
{
    public override string? CommandText { get; set; }
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    protected override DbConnection? DbConnection { get; set; }
    protected override DbParameterCollection DbParameterCollection => new ThrowingParameters();
    protected override DbTransaction? DbTransaction { get; set; }

    public override void Cancel()
    {
    }

    public override int ExecuteNonQuery() => throw exception;

    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromException<int>(exception);

    public override object? ExecuteScalar() => throw exception;

    public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromException<object?>(exception);

    public override void Prepare()
    {
    }

    protected override DbParameter CreateDbParameter() => new ThrowingParameter();

    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw exception;

    protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior,
        CancellationToken cancellationToken)
        => Task.FromException<DbDataReader>(exception);
}