using System.Reflection;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Sdk.V1;
using Supertext.Umbraco.Translation.Configuration;

namespace Supertext.Umbraco.Translation.Api;

/// <summary>
/// Creates the bundled Supertext SDK client (<see cref="SupertextClient"/>, namespace <c>Supertext.Umbraco.Translation.Sdk.V1</c>)
/// from the current options. The SDK handles the protocol: authentication header, multipart upload,
/// 429 retries, polling, download, delete and HTML packing.
/// </summary>
public sealed class SupertextClientFactory(IHttpClientFactory httpClientFactory, IOptionsMonitor<SupertextOptions> options)
{
    /// <summary>Name of the <see cref="HttpClient"/> registered for Supertext.</summary>
    public const string HttpClientName = "Supertext";

    /// <summary>The package version (the NuGet version from the .csproj, without build metadata).</summary>
    public static readonly string PackageVersion = ReadPackageVersion();

    private static readonly string UserAgent = "Umbraco-Supertext-Translation/" + PackageVersion;

    public bool HasApiKey => options.CurrentValue.EffectiveApiKey != string.Empty;

    public string ApiKey => options.CurrentValue.EffectiveApiKey;

    public string Endpoint => options.CurrentValue.EffectiveEndpoint;

    /// <summary>Client options for the SDK (also used by the version reporter's API key check).</summary>
    /// <param name="clock">For tests: the clock (default: the system clock).</param>
    public SupertextClientOptions ClientOptions(TimeProvider? clock = null)
    {
        var o = options.CurrentValue;
        return new SupertextClientOptions
        {
            BaseUrl = o.EffectiveEndpoint,
            PollInterval = TimeSpan.FromSeconds(Math.Max(1, o.PollIntervalSeconds)),
            Timeout = TimeSpan.FromSeconds(Math.Max(10, o.PollTimeoutSeconds)),
            UserAgent = UserAgent,
            HttpClient = httpClientFactory.CreateClient(HttpClientName),
            TimeProvider = clock,
        };
    }

    public SupertextClient Create() => new(options.CurrentValue.EffectiveApiKey, ClientOptions());

    private static string ReadPackageVersion()
    {
        var assembly = typeof(SupertextClientFactory).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            var plus = informational.IndexOf('+');
            return plus >= 0 ? informational[..plus] : informational;
        }
        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }
}
