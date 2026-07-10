using DevExpress.Xpo;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace LocalGPT.Controller
{
 

    [ApiController]
    [Route("__diag/database/[controller]")]
    public class DatabaseQueryController(LocalGptMemoryDbContext db, ILogger<DatabaseQueryController> logger) : ControllerBase
    {

        [HttpGet("query")]
        public async Task<IActionResult> QueryDatabase([FromQuery] string table, [FromQuery] int take = 100)
        {
            try
            {
                var result = table.ToLower() switch
                {
                    "regexpatterns" => await db.RegexPatterns.Take(take).ToListAsync(),
                    "prompts" => await db.Prompts.Take(take).ToListAsync(),
                    "systemvariables" => await db.SystemVariables.Take(take).ToListAsync(),
                    _ => throw new ArgumentException("Invalid table name")
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in QUeryDatabase table {table} take {take} ex {ex.ToString()}");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("configs")]
        public async Task<IActionResult> ListConfigs()
        {
            try
            {
                var result = new Dictionary<string, object>
        {
            { "RegexPatterns", await db.RegexPatterns.ToListAsync<RegexPattern>() },
            { "Prompts", await db.Prompts.Where(p => p.Language == "en").ToListAsync<PromptConfig>() },
            { "SystemVariables", await db.SystemVariables.ToListAsync<SystemVariable>() }
        };
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListConfigs ex {ex.ToString()}");
                return BadRequest(new { error = ex.Message });
            }
          
        }

        [HttpGet("configs/{id}")]
        public async Task<IActionResult> GetConfigEntry(string id)
        {
            try
            {
                var entity = await db.Set<IClaimsIdentity>(id).FirstOrDefaultAsync();
                if (entity == null)
                    return NotFound($"No config entry found with ID '{id}'");

                // Return appropriate data based on type
                switch (entity.Type)
                {
                    case "RegexPattern":
                        return Ok(new Regex(entity.Pattern, entity.Flags));
                    case "PromptConfig":
                        return Ok(entity.Text);
                    default:
                        return Ok(ParseValue<T>(entity.ValueString, entity.DataType));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetConfigEntry id {id.ToString()} ex {ex.ToString()}");
                return BadRequest(new { error = ex.Message });
            }
          
        }

        private static T ParseValue<T>(string valueString, string dataType) => ... // Implementation omitted

}
