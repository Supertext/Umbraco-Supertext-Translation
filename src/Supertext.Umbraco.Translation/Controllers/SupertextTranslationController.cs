using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Supertext.Umbraco.Translation.Api;
using Supertext.Umbraco.Translation.Services;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Actions;
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
    SupertextClient client,
    IAuthorizationService authorizationService,
    IBackOfficeSecurityAccessor backOfficeSecurityAccessor) : ManagementApiControllerBase
{
    public sealed record LanguageModel(string IsoCode, string Name, bool IsDefault, bool Exists);

    public sealed record StatusModel(bool HasApiKey, string Endpoint);

    public sealed record TranslateRequest(Guid DocumentId, string SourceCulture, string[] TargetCultures, bool Overwrite);

    /// <param name="Error">English message</param>
    /// <param name="ErrorCode">key suffix of <c>supertext_error_…</c> in the backoffice localization</param>
    public sealed record CultureResultModel(string Culture, string Status, int Fields, string? Error, string? ErrorCode, string[]? ErrorArgs, string? ErrorDetail);

    [HttpGet("status")]
    [ProducesResponseType<StatusModel>(StatusCodes.Status200OK)]
    public IActionResult Status() => Ok(new StatusModel(client.HasApiKey, client.Endpoint));

    [HttpGet("languages")]
    [ProducesResponseType<IEnumerable<LanguageModel>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Languages(Guid documentId)
    {
        try
        {
            var languages = await translator.GetLanguagesAsync(documentId);
            return Ok(languages.Select(l => new LanguageModel(l.IsoCode, l.Name, l.IsDefault, l.Exists)));
        }
        catch (SupertextException e)
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
            return BadRequest(ProblemFor(new SupertextException("chooseLanguages", "Choose a source language and at least one target language.")));
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
            return Ok(results.Select(r => new CultureResultModel(r.Culture, r.Status.ToString().ToLowerInvariant(), r.Fields, r.Error, r.Exception?.Code, r.Exception?.Args, r.Exception?.Detail)));
        }
        catch (SupertextException e)
        {
            return BadRequest(ProblemFor(e));
        }
    }

    /// <summary>English title plus code, arguments and detail, so the backoffice can localize it.</summary>
    private static ProblemDetails ProblemFor(SupertextException e) => new()
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
