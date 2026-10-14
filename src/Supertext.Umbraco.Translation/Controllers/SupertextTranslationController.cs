using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Configuration;
using Supertext.Umbraco.Translation.Sdk.V1;
using Supertext.Umbraco.Translation.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Security.Authorization;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Extensions;

namespace Supertext.Umbraco.Translation.Controllers;

/// <summary>Backoffice API used by the "Translate with Supertext" action.</summary>
[ApiVersion("1.0")]
[VersionedApiBackOfficeRoute("supertext")]
[ApiExplorerSettings(GroupName = "Supertext")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
public sealed class SupertextTranslationController(
    ContentTranslator translator,
    SupertextClientFactory clients,
    IAuthorizationService authorizationService,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor,
    SupertextReporting reporting,
    IOptionsMonitor<SupertextOptions> options,
    IOptionsMonitor<WebRoutingSettings> webRouting) : ManagementApiControllerBase
{
    /// <param name="SupertextCode">the Supertext code it is sent as; null when the language isn't mapped (can't be translated)</param>
    public sealed record LanguageModel(string IsoCode, string Name, bool IsDefault, bool Exists, string? SupertextCode);

    public sealed record StatusModel(bool HasApiKey, string Endpoint);

    public sealed record TranslateRequest(Guid DocumentId, string SourceCulture, string[] TargetCultures, bool Overwrite);

    /// <param name="Error">English message</param>
    /// <param name="ErrorCode">key suffix of <c>supertext_error_…</c> in the backoffice localization</param>
    public sealed record CultureResultModel(string Culture, string Status, int Fields, string? Error, string? ErrorCode, string[]? ErrorArgs, string? ErrorDetail);

    [HttpGet("status")]
    [ProducesResponseType<StatusModel>(StatusCodes.Status200OK)]
    public IActionResult Status()
    {
        Report();
        return Ok(new StatusModel(clients.HasApiKey, clients.Endpoint));
    }

    [HttpGet("languages")]
    [ProducesResponseType<IEnumerable<LanguageModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Languages(Guid documentId)
    {
        try
        {
            var languages = await translator.GetLanguagesAsync(documentId);
            Report();
            return Ok(languages.Select(l => new LanguageModel(l.IsoCode, l.Name, l.IsDefault, l.Exists, LanguageMapping.Resolve(l.IsoCode, options.CurrentValue).Code)));
        }
        catch (TranslationException e)
        {
            return NotFound(ProblemFor(e));
        }
    }

    [HttpPost("translate")]
    [ProducesResponseType<IEnumerable<CultureResultModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Translate([FromBody] TranslateRequest request, CancellationToken ct)
    {
        if (request.TargetCultures is not { Length: > 0 } || string.IsNullOrWhiteSpace(request.SourceCulture))
        {
            return BadRequest(ProblemFor(new TranslationException("chooseLanguages", "Choose a source language and at least one target language.")));
        }

        // The editor needs update rights on the document in the target languages.
        var authorized = await authorizationService.AuthorizeResourceAsync(
            User,
            ContentPermissionResource.WithKeys(ActionUpdate.ActionLetter, request.DocumentId, request.TargetCultures),
            AuthorizationPolicies.ContentPermissionByResource);
        if (!authorized.Succeeded)
        {
            return Forbidden();
        }

        var userId = backOfficeSecurityAccessor.BackOfficeSecurity?.CurrentUser?.Id ?? Constants.Security.SuperUserId;
        try
        {
            var results = await translator.TranslateAsync(request.DocumentId, request.SourceCulture, request.TargetCultures, request.Overwrite, userId, ct);
            // Supertext rejected the key: the key check becomes "invalid" (reported once, when it changes).
            var rejected = results.Select(r => r.Exception?.InnerException).OfType<SupertextException>()
                .FirstOrDefault(e => e.Code == SupertextErrorCodes.AuthenticationFailure);
            if (rejected is not null)
            {
                reporting.InBackground(ReportAction.AuthenticationFailed, SiteUrl(), rejected.Status ?? 401);
            }
            else
            {
                Report();
            }
            return Ok(results.Select(r => new CultureResultModel(r.Culture, r.Status.ToString().ToLowerInvariant(), r.Fields, r.Error, r.Exception?.Code, r.Exception?.Args, r.Exception?.Detail)));
        }
        catch (TranslationException e)
        {
            return BadRequest(ProblemFor(e));
        }
    }

    /// <summary>Version report and key check of a back-end request, in the background (Services/SupertextReporting.cs).</summary>
    private void Report() => reporting.InBackground(ReportAction.OnRequest, SiteUrl());

    private string? SiteUrl() =>
        SupertextReporting.SiteUrl(webRouting.CurrentValue.UmbracoApplicationUrl, Request.Scheme, Request.Host.Value, Request.PathBase.Value);

    /// <summary>English title plus code, arguments and detail, so the backoffice can localize it.</summary>
    private static ProblemDetails ProblemFor(TranslationException e) => new()
    {
        Title = e.Message,
        Extensions =
        {
            ["supertextCode"] = e.Code,
            ["supertextArgs"] = e.Args,
            ["supertextDetail"] = e.Detail,
        },
    };
}
