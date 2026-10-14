using Supertext.Umbraco.Translation.Configuration;
using Supertext.Umbraco.Translation.Sdk.V1;

namespace Supertext.Umbraco.Translation.Services;

/// <summary>What the package sends to Supertext for one Umbraco culture (for the Supertext dashboard and the dialog).</summary>
/// <param name="Code">The Supertext target code, or null when the culture isn't mapped (it can't be translated).</param>
/// <param name="Mapped">True when the code comes from <c>Supertext:Languages:&lt;culture&gt;:Code</c>, false for an exact match.</param>
/// <param name="Politeness">"more", "less" or "default", as configured.</param>
/// <param name="PolitenessSupported">Whether Supertext offers formal/informal for the code (null: a code the SDK doesn't list).</param>
public sealed record LanguageResolution(string? Code, bool Mapped, string Politeness, bool? PolitenessSupported);

/// <summary>
/// Umbraco cultures → Supertext codes, with the Supertext SDK's rules (the same in every Supertext plugin,
/// no guessing): the mapping <c>Supertext:Languages:&lt;culture&gt;:Code</c> wins; else a culture that is a
/// Supertext code apart from case and <c>_</c>/<c>-</c> (de-CH, es); else the SDK throws
/// <c>unsupported_language</c> and nothing is sent. One mapping serves both directions: as source, a mapped
/// code is sent in its source form (de-CH → de).
/// </summary>
public static class LanguageMapping
{
    /// <exception cref="SupertextException"><c>unsupported_language</c> when the culture isn't mapped and isn't a Supertext code.</exception>
    public static string Target(string culture, SupertextOptions options) => Languages.Target(culture, options.LanguageMapping);

    /// <exception cref="SupertextException"><c>unsupported_language</c> when the culture isn't mapped and has no Supertext source language.</exception>
    public static string Source(string culture, SupertextOptions options) => Languages.Source(culture, options.LanguageMapping);

    /// <summary>The configured tone for a target culture; sent as configured (Supertext answers <c>unsupported_feature</c> where it has none).</summary>
    public static Politeness PolitenessFor(string culture, SupertextOptions options) => PolitenessSetting(culture, options) switch
    {
        "more" => Politeness.More,
        "less" => Politeness.Less,
        _ => Politeness.Default,
    };

    public static LanguageResolution Resolve(string culture, SupertextOptions options)
    {
        var mapping = options.LanguageMapping;
        string? code;
        try
        {
            code = Languages.Target(culture, mapping);
        }
        catch (SupertextException)
        {
            code = null;
        }
        var mapped = mapping.Keys.Any(k => Same(k, culture));
        return new LanguageResolution(code, code is not null && mapped, PolitenessSetting(culture, options), code is null ? null : Languages.SupportsPoliteness(code));
    }

    private static string PolitenessSetting(string culture, SupertextOptions options)
    {
        var entry = options.Languages.FirstOrDefault(l => Same(l.Key, culture)).Value;
        return (entry?.Politeness ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "more" => "more",
            "less" => "less",
            _ => "default",
        };
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.Trim().Replace('_', '-'), b.Trim().Replace('_', '-'), StringComparison.OrdinalIgnoreCase);
}
