using LocalGPT.Services.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace LocalGPT.Controller;

[ApiController]
[Route("api/localization")]
public sealed class LocalizationController(ILocalGptLocalizationService localization, ILogger<LocalizationController> logger) : ControllerBase
{
    [HttpGet("current")]
    public ActionResult<IReadOnlyDictionary<string, string>> Current()
        => Ok(localization.GetStrings(CultureInfo.CurrentUICulture.Name));

    [HttpGet("{culture}")]
    public ActionResult<IReadOnlyDictionary<string, string>> Get(string culture)
        => Ok(localization.GetStrings(culture));

    [HttpGet("select")]
    public IActionResult Select([FromQuery] string culture, [FromQuery] string? returnUrl = "/")
    {
        var selected = localization.GetAvailableCultures()
            .FirstOrDefault(item => string.Equals(item, culture, StringComparison.OrdinalIgnoreCase)) ?? "en-US";
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(selected)),
            new CookieOptions { IsEssential = true, SameSite = SameSiteMode.Lax, Expires = DateTimeOffset.UtcNow.AddYears(1) });
        logger.LogInformation("LocalGPT UI culture changed to {Culture}.", selected);
        return LocalRedirect(string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl) ? "/" : returnUrl);
    }
}
