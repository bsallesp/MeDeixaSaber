using System.Collections.Generic;

namespace MeDeixaSaber.Core.Models;

public sealed class OutsideNews
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? CreatedAt { get; set; }
    public ICollection<Category>? Categories { get; set; } = new List<Category>();
}