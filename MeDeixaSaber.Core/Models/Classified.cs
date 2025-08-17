namespace MeDeixaSaber.Core.Models;

public sealed class Classified
{
    public long Id { get; set; }
    public DateTime? CapturedAtUtc { get; set; }
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string? RefId { get; set; }
    public string? Location { get; set; }
    public string? ListingWhen { get; set; }
    public string? PostDate { get; set; }
    public string? Phone { get; set; }
    public string? State { get; set; }
    public string? Description { get; set; }
    public bool IsDuplicate { get; set; }
}