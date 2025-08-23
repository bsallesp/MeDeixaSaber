using System.Data;
using System.Data.Common;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingCommand(Exception toThrow) : DbCommand
{
    readonly Exception _toThrow = toThrow;
    public override string CommandText { get; set; } = string.Empty;
    public override int CommandTimeout { get; set; }
    public override CommandType CommandType { get; set; }
    protected override DbConnection DbConnection { get; set; } = null!;
    protected override DbParameterCollection DbParameterCollection { get; } = new ThrowingParameters();
    protected override DbTransaction DbTransaction { get; set; } = null!;
    public override bool DesignTimeVisible { get; set; }
    public override UpdateRowSource UpdatedRowSource { get; set; }
    public override void Cancel() { }
    protected override DbParameter CreateDbParameter() => new ThrowingParameter();
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw _toThrow;
    public override int ExecuteNonQuery() => throw _toThrow;
    public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromException<int>(_toThrow);
    public override object ExecuteScalar() => throw _toThrow;
    public override Task<object> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromException<object>(_toThrow);
    public override void Prepare() { }
}