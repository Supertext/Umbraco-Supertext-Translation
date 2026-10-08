using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Configuration;

namespace Supertext.Umbraco.Translation.Api;

/// <summary>
/// An error shown to editors. <see cref="Exception.Message"/> is the English text (logs, API
/// clients); <see cref="Code"/>, <see cref="Args"/> and <see cref="Detail"/> let the backoffice
/// show it in the editor's language (keys <c>supertext_error_&lt;code&gt;</c> in
/// <c>App_Plugins/SupertextTranslation/lang/</c>).
/// </summary>
public sealed class SupertextException(string code, string message, string[]? args = null, string? detail = null, Exception? inner = null)
    : Exception(detail is null or "" ? message : message + " (" + detail + ")", inner)
{
    public string Code { get; } = code;

    public string[] Args { get; } = args ?? [];

    /// <summary>Text returned by the Supertext API (not translated).</summary>
    public string? Detail { get; } = detail is "" ? null : detail;
}

/// <summary>
/// Supertext AI file translation (https://api.supertext.com/v1/), the same protocol as the
/// WordPress, TYPO3 and Neos integrations: submit one HTML document, poll its status,
/// download the translation, delete the file.
/// </summary>
public sealed partial class SupertextClient(HttpClient http, IOptionsMonitor<SupertextOptions> options)
{
    /// <summary>Retries after HTTP 429 (requests per second are limited per key).</summary>
    private const int RateLimitRetries = 4;

    public const int MaxDocumentCharacters = 900_000;

    private SupertextOptions Options => options.CurrentValue;

    public bool HasApiKey => Options.EffectiveApiKey != string.Empty;

    public string Endpoint => Options.EffectiveEndpoint;

    /// <param name="targetLanguage">BCP-47 code, e.g. "de-CH"</param>
    /// <param name="sourceLanguage">primary subtag ("en"), empty for auto-detection</param>
    /// <param name="politeness">"default", "more" or "less"</param>
    public async Task<string> TranslateDocumentAsync(string html, string targetLanguage, string sourceLanguage, string politeness, CancellationToken ct = default)
    {
        var fileId = await SubmitAsync(html, targetLanguage, sourceLanguage, politeness, ct);
        try
        {
            await WaitUntilDoneAsync(fileId, ct);
            return await DownloadAsync(fileId, ct);
        }
        finally
        {
            await DeleteQuietlyAsync(fileId);
        }
    }

    /// <summary>Cost-free check of the API key.</summary>
    public async Task ValidateApiKeyAsync(CancellationToken ct = default)
    {
        using var _ = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, "features"), ct);
    }

    private async Task<string> SubmitAsync(string html, string targetLanguage, string sourceLanguage, string politeness, CancellationToken ct)
    {
        HttpRequestMessage Build()
        {
            var form = new MultipartFormDataContent();
            // Quoted part names (name="target_lang"), as browsers send them: .NET leaves them
            // unquoted by default, which stricter multipart parsers reject.
            void AddField(string name, string value)
            {
                var part = new StringContent(value);
                part.Headers.ContentType = null;
                part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = $"\"{name}\"" };
                form.Add(part);
            }
            AddField("target_lang", targetLanguage);
            if (sourceLanguage != string.Empty)
            {
                // Supertext expects the source as a primary subtag ("de", not "de-CH"),
                // otherwise the pair is rejected with INVALID_LANGUAGE_PAIR.
                AddField("source_lang", sourceLanguage.Split('-', '_')[0].ToLowerInvariant());
            }
            if (politeness is "more" or "less")
            {
                AddField("politeness", politeness);
            }
            // The part's Content-Type must be exactly "text/html" (no charset), otherwise 415.
            var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(html));
            file.Headers.ContentType = new MediaTypeHeaderValue("text/html");
            file.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "\"file\"", FileName = "\"content.html\"" };
            form.Add(file);
            return new HttpRequestMessage(HttpMethod.Post, "translate/ai/file") { Content = form };
        }

        using var response = await SendAsync(Build, ct);
        var fileId = (await ReadJsonAsync(response, ct)).TryGetProperty("file_id", out var id) ? id.ToString() : string.Empty;
        if (fileId == string.Empty)
        {
            throw new SupertextException("noFileId", "Supertext did not return a file id.");
        }
        return fileId;
    }

    private async Task WaitUntilDoneAsync(string fileId, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(Math.Max(10, Options.PollTimeoutSeconds));
        var interval = TimeSpan.FromSeconds(Math.Max(1, Options.PollIntervalSeconds));
        do
        {
            using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"translate/ai/file/{Uri.EscapeDataString(fileId)}/status"), ct);
            var json = await ReadJsonAsync(response, ct);
            var status = json.TryGetProperty("status", out var s) ? s.GetString() : null;
            switch (status)
            {
                case "done": return;
                case "error": throw new SupertextException("translationFailed", "Supertext failed to translate the document.");
                case "limit_exceeded": throw new SupertextException("limitExceeded", "Your Supertext translation limit is exceeded.");
                case "deleted": throw new SupertextException("fileDeleted", "The Supertext file was deleted before it could be downloaded.");
            }
            await Task.Delay(interval, ct);
        }
        while (DateTime.UtcNow < deadline);
        throw new SupertextException("timeout", "Timed out waiting for the Supertext translation.");
    }

    private async Task<string> DownloadAsync(string fileId, CancellationToken ct)
    {
        using var response = await SendAsync(() => new HttpRequestMessage(HttpMethod.Get, $"translate/ai/file/{Uri.EscapeDataString(fileId)}/translation"), ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new SupertextException("emptyTranslation", "The translated document was empty.");
        }
        return body;
    }

    private async Task DeleteQuietlyAsync(string fileId)
    {
        try
        {
            using var _ = await SendAsync(() => new HttpRequestMessage(HttpMethod.Delete, $"translate/ai/file/{Uri.EscapeDataString(fileId)}"), CancellationToken.None);
        }
        catch
        {
            // Files expire after 24 h anyway.
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Func<HttpRequestMessage> build, CancellationToken ct)
    {
        // Accept the key with or without the "Supertext-Auth-Key " prefix Supertext shows it with.
        var apiKey = AuthPrefix().Replace(Options.EffectiveApiKey, string.Empty);
        if (apiKey == string.Empty)
        {
            throw new SupertextException("noApiKey", "No Supertext API key configured (Supertext:ApiKey or SUPERTEXT_API_KEY). No Supertext account yet? Create one at https://www.supertext.com/person/en/account/signin. Generate your API key at https://www.supertext.com/en/integrations/api (supertext.com → Integrations → API, requires the Admin role).");
        }
        var baseUri = new Uri(Options.EffectiveEndpoint);

        HttpResponseMessage response;
        for (var attempt = 0; ; attempt++)
        {
            var request = build();
            request.RequestUri = new Uri(baseUri, request.RequestUri!.ToString());
            request.Headers.TryAddWithoutValidation("Authorization", "Supertext-Auth-Key " + apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            try
            {
                response = await http.SendAsync(request, ct);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
            {
                throw new SupertextException("unreachable", "Could not reach Supertext.", detail: e.Message, inner: e);
            }
            if (response.StatusCode != HttpStatusCode.TooManyRequests || attempt >= RateLimitRetries)
            {
                break;
            }
            // Rate limited: wait (Retry-After, else 1, 2, 4, 8 s with jitter) and retry.
            var delay = response.Headers.RetryAfter?.Delta
                ?? TimeSpan.FromMilliseconds(1000 * Math.Pow(2, attempt) + Random.Shared.Next(0, 250));
            response.Dispose();
            await Task.Delay(delay > TimeSpan.FromSeconds(30) ? TimeSpan.FromSeconds(30) : delay, ct);
        }

        var code = (int)response.StatusCode;
        if (code is >= 200 and < 300)
        {
            return response;
        }
        var (errorCode, message) = code switch
        {
            401 or 403 => ("authFailed", "Authentication failed. Please check the Supertext API key. No Supertext account yet? Create one at https://www.supertext.com/person/en/account/signin. Generate your API key at https://www.supertext.com/en/integrations/api (supertext.com → Integrations → API, requires the Admin role)."),
            404 => ("notFound", "The requested Supertext resource was not found."),
            413 => ("tooLarge", "The content is too large for Supertext to translate in one go."),
            429 => ("rateLimited", "Too many requests to Supertext. Please try again shortly."),
            >= 500 => ("unavailable", "The Supertext service is currently unavailable."),
            _ => ("http", $"Supertext answered with HTTP {code}."),
        };
        var detail = Tags().Replace(await response.Content.ReadAsStringAsync(ct), string.Empty).Trim();
        response.Dispose();
        throw new SupertextException(errorCode, message, [code.ToString(System.Globalization.CultureInfo.InvariantCulture)], detail.Length > 200 ? detail[..200] : detail);
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    [GeneratedRegex(@"^\s*Supertext-Auth-Key\s+", RegexOptions.IgnoreCase)]
    private static partial Regex AuthPrefix();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tags();
}
