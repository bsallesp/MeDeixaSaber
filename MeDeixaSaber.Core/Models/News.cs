namespace MeDeixaSaber.Core.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Summary { get; set; }
        public string Content { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Url { get; set; } = null!;
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}