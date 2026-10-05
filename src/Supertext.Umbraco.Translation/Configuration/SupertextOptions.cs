namespace Supertext.Umbraco.Translation.Configuration;

/// <summary>
/// Settings from the "Supertext" section of appsettings.json. The environment
/// variables SUPERTEXT_API_KEY and SUPERTEXT_API_ENDPOINT win over the file.
/// </summary>
public sealed class SupertextOptions
{
    public const string SectionName = "Supertext";

    /// <summary>Supertext API key; shown by Supertext as "Supertext-Auth-Key &lt;key&gt;", either form works.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>API base URL. Live: https://api.supertext.com/v1/ (also api.staging… / api.testing…).</summary>
    public string Endpoint { get; set; } = "https://api.supertext.com/v1/";

    /// <summary>Seconds between status checks.</summary>
    public int PollIntervalSeconds { get; set; } = 2;

    /// <summary>Maximum seconds to wait for one translated document.</summary>
    public int PollTimeoutSeconds { get; set; } = 240;

    /// <summary>
    /// Optional per-language settings keyed by Umbraco culture (ISO code, e.g. "de-CH").
    /// Without an entry the culture is sent to Supertext as is.
    /// </summary>
    public Dictionary<string, SupertextLanguageOptions> Languages { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Property editors whose string values are translated as plain text.</summary>
    public List<string> PlainTextEditors { get; set; } = ["Umbraco.TextBox", "Umbraco.TextArea"];

    /// <summary>Property editors whose values are translated as HTML (rich text, including its blocks).</summary>
    public List<string> RichTextEditors { get; set; } = ["Umbraco.RichText", "Umbraco.TinyMCE"];

    /// <summary>Block editors whose blocks are searched for translatable properties.</summary>
    public List<string> BlockEditors { get; set; } = ["Umbraco.BlockList", "Umbraco.BlockGrid", "Umbraco.SingleBlock"];

    /// <summary>Property aliases that are never translated (copied as they are).</summary>
    public List<string> ExcludedProperties { get; set; } = [];

    internal string EffectiveApiKey
    {
        get
        {
            var fromEnv = Environment.GetEnvironmentVariable("SUPERTEXT_API_KEY");
            return (string.IsNullOrWhiteSpace(fromEnv) ? ApiKey : fromEnv).Trim();
        }
    }

    internal string EffectiveEndpoint
    {
        get
        {
            var fromEnv = Environment.GetEnvironmentVariable("SUPERTEXT_API_ENDPOINT");
            var url = string.IsNullOrWhiteSpace(fromEnv) ? Endpoint : fromEnv.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://api.supertext.com/v1/";
            }
            return url.TrimEnd('/') + "/";
        }
    }
}

public sealed class SupertextLanguageOptions
{
    /// <summary>Target language code sent to Supertext, e.g. "de-CH". Default: the culture itself.</summary>
    public string? Code { get; set; }

    /// <summary>"more" (formal: Sie/vous/Lei), "less" (informal: du/tu) or "default".</summary>
    public string? Politeness { get; set; }

    /// <summary>False hides the language from the translate dialog.</summary>
    public bool Enabled { get; set; } = true;
}
