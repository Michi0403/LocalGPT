using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Controller;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller
{
    public class ChatController(IRegexPatternService regexSvc, LocalGptMemoryDbContext db, ILogger<DatabaseKnowledgeController> logger) : ControllerBase
    {
        // New service-based implementation
        [HttpGet("help-message")]
        public async Task<IActionResult> GetHelpMessage()
        {
            try
            {
                var patternName = "HelpMessage";
                var message = await regexSvc.GetRegexAsync(patternName);

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