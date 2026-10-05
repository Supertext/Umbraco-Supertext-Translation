using Supertext.Umbraco.Translation.Api;
using Xunit;

namespace Supertext.Umbraco.Translation.Tests;

public class HtmlDocumentTests
{
    private static readonly HtmlDocument.Segment[] Segments =
    [
        new("Fish & Chips < 10 CHF, \"quoted\"", false),
        new("Monday|9-18\nSaturday|10-16\n\nSunday|closed", false),
        new("<p>We ship <strong>Swiss chocolate</strong>.</p><ul><li>Fast</li></ul>", true),
        new("Grüezi – café, naïve, 日本語", false),
    ];

    [Fact]
    public void Segments_round_trip_unchanged_even_when_whitespace_changes()
    {
        // Simulate Supertext re-serialising the document with different whitespace.
        var document = HtmlDocument.Build(Segments).Replace("</div>\n", "</div>\n\n   ");
        var parsed = HtmlDocument.Parse(document, Segments);

        Assert.Equal(Segments.Length, parsed.Count);
        for (var i = 0; i < Segments.Length; i++)
        {
            Assert.Equal(Segments[i].Text, parsed[i]);
        }
    }

    [Fact]
    public void Plain_text_is_escaped_and_line_breaks_become_br()
    {
        var document = HtmlDocument.Build([new("a < b\nc", false)]);
        Assert.Contains("<div data-st-id=\"0\">a &lt; b<br>c</div>", document);
    }

    [Fact]
    public void Unknown_or_out_of_range_ids_are_ignored()
    {
        var parsed = HtmlDocument.Parse("<div data-st-id=\"7\">x</div><div data-st-id=\"0\">ok</div>", [new("a", false)]);
        Assert.Single(parsed);
        Assert.Equal("ok", parsed[0]);
    }
}
