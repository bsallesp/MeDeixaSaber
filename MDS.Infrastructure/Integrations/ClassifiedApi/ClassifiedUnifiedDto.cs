namespace MDS.Infrastructure.Integrations.ClassifiedApi;

public sealed record ClassifiedUnifiedDto
{
    public required string Title { get; init; }
    public required string PostDate { get; init; }
    public required string Description { get; init; }
    public string[]? Tags { get; init; }
    public required string Url { get; init; }
}