using System.Text.Json;

namespace MDS.Runner.NewsLlm.Journalists
{
    public static class ResponseTextExtractor
    {
        public static string? Extract(string responseJson)
        {
            if (string.IsNullOrWhiteSpace(responseJson)) return null;

            try
            {
                using var doc = JsonDocument.Parse(responseJson);

                if (doc.RootElement.TryGetProperty("output_text", out var ot) && ot.ValueKind == JsonValueKind.String)
                    return ot.GetString();

                if (doc.RootElement.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
                {
                    foreach (var msg in output.EnumerateArray())
                    {
                        if (!msg.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;

                        foreach (var part in content.EnumerateArray())
                        {
                            if (!part.TryGetProperty("type", out var typeEl) || typeEl.ValueKind != JsonValueKind.String) continue;
                            if (typeEl.GetString() != "output_text") continue;

                            if (!part.TryGetProperty("text", out var txt)) continue;

                            if (txt.ValueKind == JsonValueKind.String) return txt.GetString();

                            if (txt.ValueKind == JsonValueKind.Object &&
                                txt.TryGetProperty("value", out var val) &&
                                val.ValueKind == JsonValueKind.String)
                                return val.GetString();
                        }
                    }
                }

                return TrySliceFirstJsonObject(responseJson);
            }
            catch
            {
                return TrySliceFirstJsonObject(responseJson);
            }
        }

        static string? TrySliceFirstJsonObject(string s)
        {
            if (string.IsNullOrEmpty(s)) return null;
            var start = s.IndexOf('{');
            var end = s.LastIndexOf('}');
            return (start >= 0 && end > start) ? s.Substring(start, end - start + 1) : null;
        }
    }
}
