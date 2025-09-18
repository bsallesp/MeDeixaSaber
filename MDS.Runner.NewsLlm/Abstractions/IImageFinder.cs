namespace MDS.Runner.NewsLlm.Abstractions
{
    public interface IImageFinder
    {
        Task<string?> FindImageUrlAsync(string title, CancellationToken ct = default);
    }
}