using Microsoft.EntityFrameworkCore;

namespace MDS.Blazor.Models;

public partial class TimesheetProdDatabaseContext : DbContext
{
    public TimesheetProdDatabaseContext()
    {
    }

    public TimesheetProdDatabaseContext(DbContextOptions<TimesheetProdDatabaseContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}