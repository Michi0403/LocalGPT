using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Controller;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller
{
    /// <summary>
    /// Provides chat controller operations.
    /// </summary>
    public class ChatController(IRegexPatternService regexSvc, ILogger<ChatController> logger) : ControllerBase
    {
        // New service-based implementation
        /// <summary>
        /// Gets help message.
        /// </summary>
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