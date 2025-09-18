using System.Text.Json.Serialization;

namespace MeDeixaSaber.Core.Models
{
    public sealed class OutsideNews
    {
        public int Id { get; set; }

        public required string Title { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Summary { get; set; }
        
        public required string Content { get; set; }
        public required string Source { get; set; }
        public required string Url { get; set; }
        
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? ImageUrl { get; set; }

        // CORRIGIDO: Tornar DateTime's anuláveis para aceitar 'null' do JSON
        public DateTime? PublishedAt { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public List<Category> Categories { get; set; } = [];
    }
}