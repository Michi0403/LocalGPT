using Microsoft.AspNetCore.Mvc;

public class ChatController : ControllerBase
{
    private readonly IRegexPatternService _regexSvc;

    // Constructor injection for all required services
    public ChatController(
        ILogger<ChatController> logger,
        IRegexPatternService regexSvc)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        RegexSvc = regexSvc ?? throw new ArgumentNullException(nameof(regexSvc));

        _logger = logger;
        _regexSvc = regexSvc;
    }

    // New service-based implementation
    [HttpGet("help-message")]
    public async Task<IActionResult> GetHelpMessage()
    {
        try
        {
            var patternName = "HelpMessage";
            var message = await _regexSvc.GetRegexAsync(patternName);

            return Ok(new { content = message });
        }
        catch (Exception ex)
        {
            // Log error and return appropriate response
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private ILogger Logger { get; set; } = null!;
    private IRegexPatternService RegexSvc { get; set; } = null!;
}
