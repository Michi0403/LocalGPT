using LocalGPT.BusinessObjects;
using LocalGPT.Services.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace LocalGPT.Controller;

/// <summary>
/// Exposes localization catalogs, culture selection and validated user-catalog imports.
/// </summary>
/// <param name="localization">Reads and imports localization catalogs.</param>
/// <param name="logger">Writes bounded localization diagnostics.</param>
[ApiController]
[Route("api/localization")]
[DocumentationUpdated("2.2.8")]
public sealed class LocalizationController(ILocalGptLocalizationService localization, ILogger<LocalizationController> logger) : ControllerBase
{
    /// <summary>Returns strings for the current UI culture.</summary>
    /// <returns>The effective localization dictionary.</returns>
    [HttpGet("current")]
    public ActionResult<IReadOnlyDictionary<string, string>> Current()
        => Ok(localization.GetStrings(CultureInfo.CurrentUICulture.Name));

    /// <summary>Returns effective strings for a requested culture.</summary>
    /// <param name="culture">Requested .NET culture name.</param>
    /// <returns>The merged localization dictionary.</returns>
    [HttpGet("{culture}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Get(string culture)
        => Ok(localization.GetStrings(culture));

    /// <summary>Returns built-in and user-supplied localization catalog descriptors.</summary>
    /// <returns>The available catalog list.</returns>
    [HttpGet("catalogs")]
    public ActionResult<IReadOnlyList<LocalizationCatalogDescriptor>> Catalogs()
        => Ok(localization.GetCatalogs());

    /// <summary>
    /// Returns the import projection for the localization API surface, obtaining current application state from the controller's collaborators and translating it into the HTTP-facing result.
    /// </summary>
    /// <param name="request">Culture, JSON content and overwrite decision.</param>
    /// <param name="cancellationToken">Cancels the import.</param>
    /// <returns>A task that completes with the import result or validation error.</returns>
    [HttpPost("catalogs/import")]
    public async Task<ActionResult<LocalizationCatalogImportResult>> Import(
        [FromBody] ImportLocalizationCatalogRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await localization.ImportCatalogAsync(request.Culture, request.Json, request.Overwrite, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is InvalidDataException or IOException or CultureNotFoundException)
        {
            logger.LogInformation(exception, "A localization catalog import was rejected.");
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>Persists the requested UI culture cookie and returns to a local application route.</summary>
    /// <param name="culture">Requested culture.</param>
    /// <param name="returnUrl">Local route to reopen after setting the cookie.</param>
    /// <returns>A local redirect response.</returns>
    [HttpGet("select")]
    public IActionResult Select([FromQuery] string culture, [FromQuery] string? returnUrl = "/")
    {
        var selected = localization.ResolveAvailableCulture(culture);
        Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        Response.Headers["Pragma"] = "no-cache";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
            new CookieOptions
            {
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = "/",
                MaxAge = TimeSpan.FromDays(365),
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        var localReturnUrl = string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl;
        var redirectUrl = localization.BuildCultureRedirectUrl(localReturnUrl, selected);
        logger.LogInformation("LocalGPT UI culture changed to {Culture}; reloading {ReturnUrl} with an explicit request culture.", selected, redirectUrl);
        return LocalRedirect(redirectUrl);
    }
}

/// <summary>
/// Carries one user localization-catalog import request.
/// </summary>
[DocumentationUpdated("2.2.8")]
public sealed class ImportLocalizationCatalogRequest
{
    /// <summary>
    /// Gets or sets the culture value that forms part of the import localization catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The culture value exposed by <see cref="ImportLocalizationCatalogRequest"/>.</value>
    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON value that forms part of the import localization catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The JSON value exposed by <see cref="ImportLocalizationCatalogRequest"/>.</value>
    public string Json { get; set; } = string.Empty;

    /// <summary>Gets or sets whether an existing user catalog may be replaced.</summary>
    /// <value>The overwrite value exposed by <see cref="ImportLocalizationCatalogRequest"/>.</value>
    public bool Overwrite { get; set; }
}
