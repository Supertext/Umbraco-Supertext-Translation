using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Sdk;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Xunit;

namespace Supertext.Umbraco.Translation.Tests;

/// <summary>
/// The glue between the package and the Supertext SDK. The SDK has its own tests for the
/// protocol, HTML packing and language codes.
/// </summary>
public class SdkIntegrationTests
{
    [Fact]
    public void EverySdkErrorCodeHasABackofficeCode()
    {
        var sdkCodes = typeof(SupertextErrorCodes).GetFields()
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!);
        foreach (var code in sdkCodes)
        {
            Assert.True(TranslationException.SdkCodes.ContainsKey(code), $"no backoffice code for {code}");
        }
    }

    [Fact]
    public void SdkErrorsKeepTheirDetailOnce()
    {
        var e = TranslationException.From(new SupertextException(
            SupertextErrorCodes.UnexpectedStatus, "Supertext answered with an unexpected HTTP status.", status: 418, detail: "teapot"));
        Assert.Equal("http", e.Code);
        Assert.Equal(["418"], e.Args);
        Assert.Equal("teapot", e.Detail);
        Assert.Equal("Supertext answered with an unexpected HTTP status. (teapot)", e.Message);
    }

    [Fact]
    public async Task FactoryClientTranslatesThroughTheNamedHttpClient()
    {
        var handler = new FakeSupertext();
        var factory = new SupertextClientFactory(
            new FakeHttpClientFactory(handler),
            new FakeOptions(new SupertextOptions { ApiKey = "Supertext-Auth-Key test-key", Endpoint = "https://api.supertext.com/v1", PollIntervalSeconds = 1 }));
        Assert.True(factory.HasApiKey);
        Assert.Equal("https://api.supertext.com/v1/", factory.Endpoint);

        var result = await factory.Create().TranslateTextsAsync(new Dictionary<string, Segment>
        {
            ["0"] = new("Fish & Chips\nfor two"),
            ["1"] = new("<p>We ship <strong>Swiss chocolate</strong>.</p>", Html: true),
        }, "de_ch", "en-US", Politeness.More);

        Assert.Equal("» Fish & Chips\n» for two", result["0"]);
        Assert.Equal("<p>» We ship <strong>» Swiss chocolate</strong>» .</p>", result["1"]);
        Assert.Equal("Supertext-Auth-Key test-key", handler.Authorization);
        Assert.Contains("Umbraco-Supertext-Translation/", handler.UserAgent);
        Assert.Contains("name=\"target_lang\"\r\n\r\nde-CH", handler.Upload);
        Assert.Contains("name=\"source_lang\"\r\n\r\nen\r\n", handler.Upload);
        Assert.Contains("name=\"politeness\"\r\n\r\nmore", handler.Upload);
        Assert.True(handler.Deleted);
    }

    /// <summary>Answers like the Supertext API; "translates" by putting "» " before every text node.</summary>
    private sealed class FakeSupertext : HttpMessageHandler
    {
        public string Upload { get; private set; } = "";
        public string? Authorization { get; private set; }
        public string UserAgent { get; private set; } = "";
        public bool Deleted { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Method == HttpMethod.Post)
            {
                Upload = await request.Content!.ReadAsStringAsync(cancellationToken);
                Authorization = request.Headers.TryGetValues("Authorization", out var auth) ? auth.Single() : null;
                UserAgent = request.Headers.UserAgent.ToString();
                return Json("{\"file_id\":\"f1\"}");
            }
            if (request.Method == HttpMethod.Delete)
            {
                Deleted = true;
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            if (path.EndsWith("/status"))
            {
                return Json("{\"status\":\"done\"}");
            }
            var start = Upload.IndexOf("<body>", StringComparison.Ordinal);
            var body = Upload[start..(Upload.IndexOf("</body>", StringComparison.Ordinal) + 7)];
            var translated = System.Text.RegularExpressions.Regex.Replace(body, ">([^<]+)<", m => ">» " + m.Groups[1].Value + "<");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("<html>" + translated + "</html>", Encoding.UTF8, "text/html") };
        }

        private static HttpResponseMessage Json(string json) =>
            new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    private sealed class FakeHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            Assert.Equal(SupertextClientFactory.HttpClientName, name);
            return new HttpClient(handler, disposeHandler: false);
        }
    }

    private sealed class FakeOptions(SupertextOptions value) : IOptionsMonitor<SupertextOptions>
    {
        public SupertextOptions CurrentValue => value;
        public SupertextOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<SupertextOptions, string?> listener) => null;
    }
}
