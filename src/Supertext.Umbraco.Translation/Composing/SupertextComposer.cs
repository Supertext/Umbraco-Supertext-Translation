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
        builder.Services.AddHttpClient<SupertextClient>(http => http.Timeout = TimeSpan.FromSeconds(60));
        builder.Services.AddScoped<ContentTranslator>();
    }
}
