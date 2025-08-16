namespace ScrapperApi.Models;

public record ClassifiedRecord(
    DateTime CapturedAtUtc,
    string Url,
    string Title,
    int? RefId,
    string Location,
    string When,
    DateTime? PostDate,
    string Phone,
    string State,
    string Description
);