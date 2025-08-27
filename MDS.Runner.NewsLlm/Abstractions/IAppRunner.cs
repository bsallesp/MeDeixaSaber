namespace MDS.Runner.NewsLlm.Abstractions;

public interface IAppRunner
{
    Task<int> RunAsync(CancellationToken ct = default);
}