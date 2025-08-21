using System.Text;
using System.Globalization;

namespace MeDeixaSaber.Core.Services;

public interface ITitleNormalizationService
{
    string Normalize(string? title);
}

public class TitleNormalizationService : ITitleNormalizationService
{
    public string Normalize(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        var trimmed = title.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var prevSpace = false;
        foreach (var c in trimmed.Select(ch => char.IsWhiteSpace(ch) ? ' ' : ch))
        {
            if (c == ' ')
            {
                if (prevSpace) continue;
                prevSpace = true;
            }
            else prevSpace = false;
            sb.Append(c);
        }
        var decomposed = sb.ToString().Normalize(NormalizationForm.FormD);
        var noMarks = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark))
        {
            noMarks.Append(ch);
        }
        return noMarks.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
}