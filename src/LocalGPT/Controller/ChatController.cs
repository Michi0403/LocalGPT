using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Controller;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller
{
    /// <summary>
    /// Exposes the chat application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    /// <param name="regexSvc">Regex pattern service dependency used by the chat workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    public class ChatController(IRegexPatternService regexSvc, ILogger<ChatController> logger) : ControllerBase
    {
        // New service-based implementation
        /// <summary>
        /// Retrieves help message for the chat API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("help-message")]
        public async Task<IActionResult> GetHelpMessage()
        {
            try
            {
                var patternName = "HelpMessage";
                var message = await regexSvc.GetRegexAsync(patternName).ConfigureAwait(false);

                return Ok(new { content = message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetHelpMessage ex {ex.ToString()}");
                // Log error and return appropriate response
                return StatusCode(500, new { error = ex.Message });
            }
        }
        //Placeholder
    }
}
