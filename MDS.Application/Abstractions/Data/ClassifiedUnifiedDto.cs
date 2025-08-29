namespace MDS.Application.Abstractions.Data;

public sealed record ClassifiedUnifiedDto(
    string Title,
    string PostDate,
    string Description,
    string[]? Tags,
    string Url
);