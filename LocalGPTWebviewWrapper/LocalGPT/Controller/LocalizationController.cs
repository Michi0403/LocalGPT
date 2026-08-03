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
[DocumentationUpdated("2.2.1")]
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

    /// <summary>Validates and imports a persistent user localization catalog.</summary>
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
        var selected = localization.GetAvailableCultures()
            .FirstOrDefault(item => string.Equals(item, culture, StringComparison.OrdinalIgnoreCase)) ?? "en-US";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
            new CookieOptions
            {
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddYears(1)
            });
        logger.LogInformation("LocalGPT UI culture changed to {Culture}.", selected);
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }
}

/// <summary>
/// Carries one user localization-catalog import request.
/// </summary>
[DocumentationUpdated("2.2.1")]
public sealed class ImportLocalizationCatalogRequest
{
    /// <summary>Gets or sets the requested .NET culture name.</summary>
    public string Culture { get; set; } = string.Empty;

    /// <summary>Gets or sets the string-to-string JSON object.</summary>
    public string Json { get; set; } = string.Empty;

    /// <summary>Gets or sets whether an existing user catalog may be replaced.</summary>
    public bool Overwrite { get; set; }
}
