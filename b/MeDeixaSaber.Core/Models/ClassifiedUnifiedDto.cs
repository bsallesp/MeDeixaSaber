namespace MeDeixaSaber.Core.Models;

public sealed record ClassifiedUnifiedDto(
    string Title,
    string PostDate,
    string Description,
    string Tag,
    string Url
);
