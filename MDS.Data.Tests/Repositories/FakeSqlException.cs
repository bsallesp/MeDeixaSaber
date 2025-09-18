using System.Data.Common;

namespace MDS.Data.Tests.Repositories;

public sealed class FakeSqlException(int number) : DbException
{
    public int Number { get; } = number;
}