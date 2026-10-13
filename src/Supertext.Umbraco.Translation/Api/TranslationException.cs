using Supertext.Sdk;

namespace Supertext.Umbraco.Translation.Api;

/// <summary>
/// An error the backoffice shows to the editor. <see cref="Code"/> is the key suffix of
/// <c>supertext_error_…</c> in the backoffice localization (lang/*.js), <see cref="Args"/> its
/// arguments, <see cref="Detail"/> untranslated text from Supertext. The message is English (logs).
/// </summary>
public sealed class TranslationException(string code, string message, string[]? args = null, string? detail = null, Exception? inner = null)
    : Exception(detail is null or "" ? message : message + " (" + detail + ")", inner)
{
    public string Code { get; } = code;

    public string[] Args { get; } = args ?? [];

    /// <summary>Text returned by the Supertext API (not translated).</summary>
    public string? Detail { get; } = detail is "" ? null : detail;

    /// <summary>Backoffice code per Supertext SDK error code (<see cref="SupertextErrorCodes"/>).</summary>
    public static readonly IReadOnlyDictionary<string, string> SdkCodes = new Dictionary<string, string>
    {
        [SupertextErrorCodes.MissingApiKey] = "noApiKey",
        [SupertextErrorCodes.AuthenticationFailure] = "authFailed",
        [SupertextErrorCodes.InvalidLanguagePair] = "languagePair",
        [SupertextErrorCodes.NotFound] = "notFound",
        [SupertextErrorCodes.PayloadTooLarge] = "tooLarge",
        [SupertextErrorCodes.TooManyRequests] = "rateLimited",
        [SupertextErrorCodes.ServiceUnavailable] = "unavailable",
        [SupertextErrorCodes.UnexpectedStatus] = "http",
        [SupertextErrorCodes.TransportError] = "unreachable",
        [SupertextErrorCodes.NoFileId] = "noFileId",
        [SupertextErrorCodes.TranslationError] = "translationFailed",
        [SupertextErrorCodes.QuotaExceeded] = "limitExceeded",
        [SupertextErrorCodes.FileDeleted] = "fileDeleted",
        [SupertextErrorCodes.Timeout] = "timeout",
        [SupertextErrorCodes.IncompleteResponse] = "emptyTranslation",
    };

    /// <summary>Wraps an error of the Supertext SDK for the backoffice.</summary>
    public static TranslationException From(SupertextException e)
    {
        var code = SdkCodes.TryGetValue(e.Code, out var c) ? c : "http";
        string[] args = code == "http" ? [e.Status?.ToString() ?? "?"] : [];
        // The SDK's message already ends with " (detail)"; this class appends the detail itself.
        var suffix = e.Detail is null ? null : " (" + e.Detail + ")";
        var message = suffix is not null && e.Message.EndsWith(suffix, StringComparison.Ordinal) ? e.Message[..^suffix.Length] : e.Message;
        return new TranslationException(code, message, args, e.Detail, e);
    }
}
