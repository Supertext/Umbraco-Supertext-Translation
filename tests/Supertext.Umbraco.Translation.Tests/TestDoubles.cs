using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Supertext.Umbraco.Translation.Sdk.V1;
using Supertext.Umbraco.Translation.Services;
using Xunit;

namespace Supertext.Umbraco.Translation.Tests;

internal sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        Assert.Equal(SupertextClientFactory.HttpClientName, name);
        return new HttpClient(handler, disposeHandler: false);
    }
}

internal sealed class FakeOptions(SupertextOptions value) : IOptionsMonitor<SupertextOptions>
{
    public SupertextOptions CurrentValue => value;
    public SupertextOptions Get(string? name) => value;
    public IDisposable? OnChange(Action<SupertextOptions, string?> listener) => null;
}

/// <summary>The reporter state in memory (instead of Umbraco's key-value table).</summary>
internal sealed class MemoryStateStore : IReporterStateStore
{
    public string? Json { get; set; }
    public int Saves { get; private set; }
    public bool Fail { get; set; }

    public ReporterState Load() => Fail ? throw new InvalidOperationException("database down") : ReporterState.FromJson(Json);

    public void Save(ReporterState state)
    {
        if (Fail)
        {
            throw new InvalidOperationException("database down");
        }
        Json = state.ToJson();
        Saves++;
    }
}
