namespace MDS.Application.Abstractions.Data;

public sealed record ClassifiedsUnifiedItem(
    string Title,
    string PostDate,
    string Description,
    string[]? Tags,
    string Url
);
