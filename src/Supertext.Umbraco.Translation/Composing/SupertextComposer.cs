using Microsoft.Extensions.DependencyInjection;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Supertext.Umbraco.Translation.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Supertext.Umbraco.Translation.Composing;

/// <summary>Registers the package; picked up by Umbraco automatically.</summary>
public sealed class SupertextComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<SupertextOptions>(builder.Config.GetSection(SupertextOptions.SectionName));
        // The SDK limits each request to 30 s itself; the HttpClient must allow at least that.
        builder.Services.AddHttpClient(SupertextClientFactory.HttpClientName, http => http.Timeout = TimeSpan.FromSeconds(60));
        builder.Services.AddSingleton<SupertextClientFactory>();
        builder.Services.AddScoped<ContentTranslator>();
    }
}
