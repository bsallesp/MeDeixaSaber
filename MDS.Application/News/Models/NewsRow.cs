namespace MDS.Application.News.Models;

public sealed record NewsRow(
    int Id,
    string Title,
    string? Summary,
    string Content,
    string Source,
    string Url,
    DateTime PublishedAt,
    DateTime CreatedAt,
    string? ImageUrl
);