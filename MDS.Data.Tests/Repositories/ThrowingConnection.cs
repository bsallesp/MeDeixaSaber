using System.Data;
using System.Data.Common;

namespace MDS.Data.Tests.Repositories;

public sealed class ThrowingConnection(Exception toThrow) : DbConnection
{
    readonly Exception _toThrow = toThrow;
    public override string ConnectionString { get; set; } = string.Empty;
    public override string Database => "db";
    public override string DataSource => "src";
    public override string ServerVersion => "v";
    public override ConnectionState State => ConnectionState.Open;
    public override void ChangeDatabase(string databaseName) { }
    public override void Close() { }
    public override void Open() { }
    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => null!;
    protected override DbCommand CreateDbCommand() => new ThrowingCommand(_toThrow);
}