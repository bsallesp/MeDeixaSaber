namespace MDS.Application.Time;

public interface IClock
{
    DateTime UtcNow { get; }
}