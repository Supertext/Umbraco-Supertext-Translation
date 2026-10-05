using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Extensions;

namespace Supertext.Umbraco.Translation.Services;

public enum TranslationStatus
{
    Translated,
    /// <summary>The target language already exists and overwrite was not requested.</summary>
    Exists,
    Failed,
}

public sealed record CultureTranslationResult(string Culture, TranslationStatus Status, int Fields, string? Error);

/// <summary>
/// Translates a document from one culture into others: copies every culture-variant
/// property value from the source culture, translates the text in it (text boxes, text
/// areas, rich text and the text inside block list / block grid blocks, including rich
/// text blocks), and saves the document. The target languages are saved as drafts; the
/// editor reviews and publishes them.
/// </summary>
public sealed class ContentTranslator(
    IContentService contentService,
    IContentTypeService contentTypeService,
    ILanguageService languageService,
    SupertextClient client,
    IOptionsMonitor<SupertextOptions> options,
    ILogger<ContentTranslator> logger)
{
    private SupertextOptions Options => options.CurrentValue;

    public async Task<IReadOnlyList<CultureTranslationResult>> TranslateAsync(
        Guid documentKey, string sourceCulture, IReadOnlyCollection<string> targetCultures, bool overwrite, int userId, CancellationToken ct = default)
    {
        var content = contentService.GetById(documentKey) ?? throw new SupertextException("Document not found.");
        if (!content.ContentType.VariesByCulture())
        {
            throw new SupertextException($"The document type \"{content.ContentType.Name}\" does not vary by culture, so it has no separate language versions to translate into.");
        }
        if (!content.IsCultureAvailable(sourceCulture))
        {
            throw new SupertextException($"The document has no {sourceCulture} version to translate from.");
        }

        var results = new List<CultureTranslationResult>();
        var changed = false;
        foreach (var target in targetCultures.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(target, sourceCulture, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (content.IsCultureAvailable(target) && !overwrite)
            {
                results.Add(new CultureTranslationResult(target, TranslationStatus.Exists, 0, null));
                continue;
            }
            try
            {
                var fields = await TranslateCultureAsync(content, sourceCulture, target, ct);
                results.Add(new CultureTranslationResult(target, TranslationStatus.Translated, fields, null));
                changed = true;
                logger.LogInformation("Supertext: translated {Fields} field(s) of document {Document} from {Source} to {Target}.", fields, documentKey, sourceCulture, target);
            }
            catch (Exception e) when (e is SupertextException or JsonException or HttpRequestException)
            {
                results.Add(new CultureTranslationResult(target, TranslationStatus.Failed, 0, e.Message));
                logger.LogError(e, "Supertext: translating document {Document} from {Source} to {Target} failed.", documentKey, sourceCulture, target);
            }
        }

        if (changed)
        {
            var saved = contentService.Save(content, userId);
            if (!saved.Success)
            {
                throw new SupertextException("Umbraco could not save the translated document: " + saved.Result);
            }
        }
        return results;
    }

    /// <returns>number of translated fields</returns>
    private async Task<int> TranslateCultureAsync(IContent content, string source, string target, CancellationToken ct)
    {
        var segments = new SegmentList();
        var writes = new List<(string Alias, Func<object?> Value)>();

        var name = content.GetCultureName(source) ?? content.Name ?? string.Empty;
        var nameIndex = segments.Add(name, false);

        foreach (var property in content.Properties)
        {
            if (!property.PropertyType.VariesByCulture())
            {
                continue; // shared by all languages
            }
            var value = property.GetValue(source);
            var editor = property.PropertyType.PropertyEditorAlias;
            var rebuild = Options.ExcludedProperties.Contains(property.Alias, StringComparer.OrdinalIgnoreCase)
                ? () => value
                : Collect(value, editor, segments);
            writes.Add((property.Alias, rebuild));
        }

        if (segments.Count > 0)
        {
            var language = Options.Languages.TryGetValue(target, out var l) ? l : null;
            var code = string.IsNullOrWhiteSpace(language?.Code) ? target : language!.Code!;
            var politeness = language?.Politeness ?? "default";
            foreach (var (offset, chunk) in segments.Chunks(SupertextClient.MaxDocumentCharacters))
            {
                var html = await client.TranslateDocumentAsync(HtmlDocument.Build(chunk), code, source, politeness, ct);
                foreach (var (id, text) in HtmlDocument.Parse(html, chunk))
                {
                    segments.SetResult(offset + id, text);
                }
            }
        }

        var translatedName = segments.Result(nameIndex);
        content.SetCultureName(string.IsNullOrWhiteSpace(translatedName) ? name : translatedName, target);
        foreach (var (alias, rebuild) in writes)
        {
            content.SetValue(alias, rebuild(), target);
        }
        return segments.TranslatedCount;
    }

    /// <summary>
    /// Registers the translatable text in a property value and returns a function that
    /// builds the translated value once the results are in.
    /// </summary>
    private Func<object?> Collect(object? value, string editorAlias, SegmentList segments)
    {
        if (value is null)
        {
            return () => null;
        }
        if (Options.PlainTextEditors.Contains(editorAlias, StringComparer.OrdinalIgnoreCase))
        {
            return CollectText(value, false, segments);
        }
        if (Options.RichTextEditors.Contains(editorAlias, StringComparer.OrdinalIgnoreCase))
        {
            return CollectRichText(value, segments);
        }
        if (Options.BlockEditors.Contains(editorAlias, StringComparer.OrdinalIgnoreCase))
        {
            return CollectBlocks(value, segments);
        }
        return () => value; // images, toggles, pickers…: copied as they are
    }

    private static Func<object?> CollectText(object value, bool isHtml, SegmentList segments)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text) || (!isHtml && NotTranslatable(text)))
        {
            return () => value;
        }
        var index = segments.Add(text, isHtml);
        return () => segments.Result(index) ?? text;
    }

    /// <summary>A bare URL, e-mail address, path or number: copied, never translated.</summary>
    private static bool NotTranslatable(string text)
    {
        var t = text.Trim();
        return !t.Any(char.IsLetter)
            || (!t.Contains(' ') && (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || t.StartsWith('/')
                || (t.Contains('@') && t.Contains('.'))));
    }

    /// <summary>
    /// Inside blocks Umbraco may hand values over encoded twice: a JSON string whose content is
    /// JSON ("{\u0022markup\u0022: …"). Unwraps that level and returns a function that wraps
    /// the result back the same way.
    /// </summary>
    private static (string Raw, Func<string, object> Wrap) Unwrap(object value)
    {
        var raw = value as string ?? (value as JsonNode)?.ToJsonString() ?? string.Empty;
        var trimmed = raw.TrimStart();
        if (trimmed.StartsWith('"'))
        {
            try
            {
                if (JsonSerializer.Deserialize<string>(trimmed) is { } inner)
                {
                    return (inner, result => JsonSerializer.Serialize(result));
                }
            }
            catch (JsonException)
            {
                // not double-encoded after all
            }
        }
        return (raw, result => result);
    }

    /// <summary>Rich text: {"markup": "...", "blocks": {...}} since Umbraco 14, or plain HTML.</summary>
    private Func<object?> CollectRichText(object value, SegmentList segments)
    {
        var (raw, wrap) = Unwrap(value);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return () => value;
        }
        if (!raw.TrimStart().StartsWith('{'))
        {
            var html = CollectText(raw, true, segments);
            return () => html() is string t ? wrap(t) : value;
        }
        var node = JsonNode.Parse(raw) as JsonObject;
        if (node is null)
        {
            return () => value;
        }
        var markup = node["markup"]?.GetValue<string>();
        int? index = string.IsNullOrWhiteSpace(markup) ? null : segments.Add(markup, true);
        var blocks = node["blocks"] is JsonObject b ? CollectBlockNode(b, segments) : null;
        return () =>
        {
            if (index is { } i && segments.Result(i) is { } translated)
            {
                node["markup"] = translated;
            }
            blocks?.Invoke();
            return wrap(node.ToJsonString());
        };
    }

    /// <summary>Block list / grid / single block: JSON with contentData[].values[].</summary>
    private Func<object?> CollectBlocks(object value, SegmentList segments)
    {
        var (raw, wrap) = Unwrap(value);
        if (string.IsNullOrWhiteSpace(raw) || JsonNode.Parse(raw) is not JsonObject node)
        {
            return () => value;
        }
        var apply = CollectBlockNode(node, segments);
        return () =>
        {
            apply();
            return wrap(node.ToJsonString());
        };
    }

    private Action CollectBlockNode(JsonObject node, SegmentList segments)
    {
        var applies = new List<Action>();
        // Only content is translated; settingsData holds layout options.
        if (node["contentData"] is not JsonArray items)
        {
            return () => { };
        }
        foreach (var item in items.OfType<JsonObject>())
        {
            if (!Guid.TryParse(item["contentTypeKey"]?.GetValue<string>(), out var typeKey)
                || contentTypeService.Get(typeKey) is not { } elementType
                || item["values"] is not JsonArray values)
            {
                continue;
            }
            var propertyTypes = elementType.CompositionPropertyTypes.ToDictionary(p => p.Alias, StringComparer.OrdinalIgnoreCase);
            foreach (var entry in values.OfType<JsonObject>())
            {
                var alias = entry["alias"]?.GetValue<string>();
                if (alias is null || !propertyTypes.TryGetValue(alias, out var propertyType)
                    || Options.ExcludedProperties.Contains(alias, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                var editor = entry["editorAlias"]?.GetValue<string>() ?? propertyType.PropertyEditorAlias;
                var inner = entry["value"];
                // The arms are cast to object on purpose: JsonNode converts implicitly from
                // string, so without the casts the switch is typed JsonNode and the string
                // turns back into a JSON value.
                object? innerValue = inner switch
                {
                    JsonValue v when v.GetValueKind() == JsonValueKind.String => (object?)v.GetValue<string>(),
                    JsonObject or JsonArray => (object?)inner,
                    _ => null,
                };
                if (innerValue is null)
                {
                    continue;
                }
                var rebuild = Collect(innerValue, editor, segments);
                var wasString = innerValue is string;
                applies.Add(() =>
                {
                    var result = rebuild();
                    entry["value"] = result switch
                    {
                        null => null,
                        string s when wasString => JsonValue.Create(s),
                        string s => JsonNode.Parse(s),
                        JsonNode n => n.DeepClone(),
                        _ => JsonValue.Create(result.ToString()),
                    };
                });
            }
        }
        return () => applies.ForEach(a => a());
    }

    /// <summary>Languages a document can be translated into, for the dialog.</summary>
    public async Task<IReadOnlyList<(string IsoCode, string Name, bool IsDefault, bool Exists)>> GetLanguagesAsync(Guid documentKey)
    {
        var content = contentService.GetById(documentKey) ?? throw new SupertextException("Document not found.");
        var languages = await languageService.GetAllAsync();
        return languages
            .Where(l => !Options.Languages.TryGetValue(l.IsoCode, out var o) || o.Enabled)
            .Select(l => (l.IsoCode, l.CultureName, l.IsDefault, content.IsCultureAvailable(l.IsoCode)))
            .ToList();
    }

    private sealed class SegmentList
    {
        private readonly List<HtmlDocument.Segment> _segments = [];
        private readonly Dictionary<int, string> _results = [];

        public int Count => _segments.Count;

        public int TranslatedCount => _results.Count;

        public int Add(string text, bool isHtml)
        {
            _segments.Add(new HtmlDocument.Segment(text, isHtml));
            return _segments.Count - 1;
        }

        public void SetResult(int index, string text)
        {
            if (!string.IsNullOrWhiteSpace(text))
            {
                _results[index] = text;
            }
        }

        public string? Result(int index) => _results.TryGetValue(index, out var t) ? t : null;

        /// <summary>Documents below Supertext's size limit: (first index, segments).</summary>
        public IEnumerable<(int Offset, List<HtmlDocument.Segment> Segments)> Chunks(int maxCharacters)
        {
            var start = 0;
            var size = 0;
            var current = new List<HtmlDocument.Segment>();
            for (var i = 0; i < _segments.Count; i++)
            {
                var length = _segments[i].Text.Length + 40;
                if (current.Count > 0 && size + length > maxCharacters)
                {
                    yield return (start, current);
                    start = i;
                    size = 0;
                    current = [];
                }
                current.Add(_segments[i]);
                size += length;
            }
            if (current.Count > 0)
            {
                yield return (start, current);
            }
        }
    }
}
