using Microsoft.Extensions.Options;
using Supertext.Sdk;
using Supertext.Umbraco.Translation.Configuration;

namespace Supertext.Umbraco.Translation.Api;

/// <summary>
/// Creates the official Supertext SDK client (<see cref="SupertextClient"/>, NuGet <c>Supertext.Sdk</c>)
/// from the current options. The SDK handles the protocol: authentication header, multipart upload,
/// 429 retries, polling, download, delete, HTML packing and language codes.
/// </summary>
public sealed class SupertextClientFactory(IHttpClientFactory httpClientFactory, IOptionsMonitor<SupertextOptions> options)
{
    /// <summary>Name of the <see cref="HttpClient"/> registered for Supertext.</summary>
    public const string HttpClientName = "Supertext";

    private static readonly string UserAgent =
        "Umbraco-Supertext-Translation/" + (typeof(SupertextClientFactory).Assembly.GetName().Version?.ToString(3) ?? "0");

    public bool HasApiKey => options.CurrentValue.EffectiveApiKey != string.Empty;

    public string Endpoint => options.CurrentValue.EffectiveEndpoint;

    public SupertextClient Create()
    {
        var o = options.CurrentValue;
        return new SupertextClient(o.EffectiveApiKey, new SupertextClientOptions
        {
            BaseUrl = o.EffectiveEndpoint,
            PollInterval = TimeSpan.FromSeconds(Math.Max(1, o.PollIntervalSeconds)),
            Timeout = TimeSpan.FromSeconds(Math.Max(10, o.PollTimeoutSeconds)),
            UserAgent = UserAgent,
            HttpClient = httpClientFactory.CreateClient(HttpClientName),
        });
    }
}
