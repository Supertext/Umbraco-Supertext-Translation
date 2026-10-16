using System.Text.RegularExpressions;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Supertext.Umbraco.Translation.PluginHealth;
using Supertext.Umbraco.Translation.Sdk.V1;
using Supertext.Umbraco.Translation.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Supertext.Umbraco.Translation.Controllers;

/// <summary>
/// Backoffice API of the Supertext dashboard (Settings section): the configuration as the package sees it,
/// the latest API key check, the version reports setting and the language mapping. Settings themselves live
/// in appsettings.json / the environment.
/// </summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("supertext")]
[ApiExplorerSettings(GroupName = "Supertext")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public sealed partial class SupertextSettingsController(
    SupertextClientFactory clients,
    SupertextReporting reporting,
    ILanguageService languageService,
    IOptionsMonitor<SupertextOptions> options,
    IOptionsMonitor<WebRoutingSettings> webRouting) : ManagementApiControllerBase
{
    public const string ReleasesUrl = "https://github.com/Supertext/Umbraco-Supertext-Translation/releases/tag/v";

    /// <param name="Status">ok, invalid, missing, unreachable or error</param>
    /// <param name="CheckedAt">ISO 8601</param>
    public sealed record KeyCheckModel(string Status, string CheckedAt, int? HttpStatus, string? ErrorCode);

    /// <param name="Enabled">whether reports are sent</param>
    /// <param name="FromEnvironment">true when SUPERTEXT_VERSION_REPORTS decides (else Supertext:SendVersionReports)</param>
    /// <param name="PingUrl">the public ping route (null while reports are off or the site URL is unknown)</param>
    public sealed record ReportsModel(bool Enabled, bool FromEnvironment, string? PingUrl);

    /// <param name="SupertextCode">null: not mapped</param>
    /// <param name="Mapped">the code comes from Supertext:Languages:&lt;culture&gt;:Code</param>
    /// <param name="Politeness">more, less or default</param>
    /// <param name="PolitenessSupported">false: Supertext offers no formal/informal for the code</param>
    /// <param name="Enabled">false: hidden in the dialog</param>
    public sealed record LanguageModel(string IsoCode, string Name, bool IsDefault, string? SupertextCode, bool Mapped, string Politeness, bool? PolitenessSupported, bool Enabled);

    /// <param name="ReleaseUrl">GitHub release of this version (X.Y.Z versions only)</param>
    public sealed record SettingsModel(
        string Version,
        string? ReleaseUrl,
        string? UmbracoVersion,
        string SdkVersion,
        bool HasApiKey,
        string Endpoint,
        KeyCheckModel? KeyCheck,
        ReportsModel Reports,
        IEnumerable<LanguageModel> Languages);

    [HttpGet("settings")]
    [ProducesResponseType<SettingsModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Settings()
    {
        var siteUrl = SiteUrl();
        reporting.InBackground(ReportAction.OnRequest, siteUrl);
        var o = options.CurrentValue;
        var languages = (await languageService.GetAllAsync()).Select(l =>
        {
            var r = LanguageMapping.Resolve(l.IsoCode, o);
            var enabled = !o.Languages.TryGetValue(l.IsoCode, out var entry) || entry.Enabled;
            return new LanguageModel(l.IsoCode, l.CultureName, l.IsDefault, r.Code, r.Mapped, r.Politeness, r.PolitenessSupported, enabled);
        }).ToList();
        var version = reporting.Installation.PackageVersion;
        return Ok(new SettingsModel(
            version,
            ReleaseVersion().IsMatch(version) ? ReleasesUrl + version : null,
            reporting.Installation.UmbracoVersion,
            Sdk.SdkInfo.Version,
            clients.HasApiKey,
            clients.Endpoint,
            ToModel(reporting.LastCheck()),
            Reports(siteUrl),
            languages));
    }

    /// <summary>"Check API key": the SDK's cost-free key check; stored, shown, and reported when its status changed.</summary>
    [HttpPost("settings/check-key")]
    [ProducesResponseType<KeyCheckModel>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckKey(CancellationToken ct)
    {
        var result = await reporting.RunAsync(ReportAction.TestConnection, SiteUrl());
        if (result?.Check is { } check)
        {
            return Ok(ToModel(check));
        }
        var sdkCheck = await clients.Create().CheckApiKeyAsync(ct);
        return Ok(new KeyCheckModel(sdkCheck.Status, sdkCheck.CheckedAt, sdkCheck.HttpStatus, sdkCheck.ErrorCode));
    }

    private ReportsModel Reports(string? siteUrl) =>
        new(reporting.Enabled, SupertextReporting.SetByEnvironment, reporting.Enabled && siteUrl is not null ? SupertextReporting.PingUrl(siteUrl) : null);

    private static KeyCheckModel? ToModel(KeyCheck? check) =>
        check is null ? null : new KeyCheckModel(check.Status, check.CheckedAt, check.HttpStatus, check.ErrorCode);

    private string? SiteUrl() =>
        SupertextReporting.SiteUrl(webRouting.CurrentValue.UmbracoApplicationUrl, Request.Scheme, Request.Host.Value, Request.PathBase.Value);

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex ReleaseVersion();
}
