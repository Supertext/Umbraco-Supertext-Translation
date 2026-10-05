using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Supertext.Umbraco.Translation.Api;

/// <summary>
/// Packs many strings into one HTML document and splits the translated document back
/// apart. Supertext keeps markup and attributes and translates text nodes, so every
/// segment travels inside &lt;div data-st-id="N"&gt;…&lt;/div&gt;.
///
/// Plain-text segments are escaped (line breaks sent as &lt;br&gt;) so they survive the round
/// trip unchanged; HTML segments (rich text) are sent as they are: one segment per field,
/// formatting as inline tags inside it.
/// </summary>
public static partial class HtmlDocument
{
    public sealed record Segment(string Text, bool IsHtml);

    public static string Build(IReadOnlyList<Segment> segments)
    {
        var sb = new StringBuilder("<!DOCTYPE html>\n<html><head><meta charset=\"utf-8\"></head><body>\n");
        for (var i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            var content = s.IsHtml
                ? s.Text
                : WebUtility.HtmlEncode(s.Text.Replace("\r\n", "\n").Replace('\r', '\n')).Replace("\n", "<br>");
            sb.Append("<div data-st-id=\"").Append(i).Append("\">").Append(content).Append("</div>\n");
        }
        return sb.Append("</body></html>").ToString();
    }

    /// <returns>segment index => translated text</returns>
    public static Dictionary<int, string> Parse(string html, IReadOnlyList<Segment> segments)
    {
        var doc = new HtmlAgilityPack.HtmlDocument { OptionOutputOriginalCase = true };
        doc.LoadHtml(html);
        var result = new Dictionary<int, string>();
        var nodes = doc.DocumentNode.SelectNodes("//div[@data-st-id]");
        if (nodes is null)
        {
            return result;
        }
        foreach (var node in nodes)
        {
            if (!int.TryParse(node.GetAttributeValue("data-st-id", ""), out var id) || id < 0 || id >= segments.Count)
            {
                continue;
            }
            if (segments[id].IsHtml)
            {
                result[id] = node.InnerHtml.Trim();
                continue;
            }
            // Plain text: <br> are the real line breaks; other whitespace (incl. formatting
            // newlines Supertext may add) collapses to one space.
            foreach (var br in node.SelectNodes(".//br")?.ToList() ?? [])
            {
                br.ParentNode.ReplaceChild(doc.CreateTextNode("\u001E"), br);
            }
            var text = WebUtility.HtmlDecode(node.InnerText);
            text = Whitespace().Replace(text, " ");
            text = LineBreak().Replace(text, "\n");
            result[id] = text.Trim(' ');
        }
        return result;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(" ?\u001E ?")]
    private static partial Regex LineBreak();
}
