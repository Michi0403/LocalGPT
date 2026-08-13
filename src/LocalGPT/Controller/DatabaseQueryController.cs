
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;
namespace LocalGPT.Controller
{


    /// <summary>
    /// Exposes the database query application operations through the web/API boundary and delegates domain work to the corresponding LocalGPT services.
    /// </summary>
    /// <param name="db">Database value supplied to the database query operation and used when producing its result.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    [ApiController]
    [Route("__diag/database/[controller]")]
    public class DatabaseQueryController(LocalGptMemoryDbContext db, ILogger<DatabaseQueryController> logger) : ControllerBase
    {

        /// <summary>
        /// Queries database for the database query API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <param name="table">Table value supplied to the database query operation and used when producing its result.</param>
        /// <param name="take">Take value supplied to the database query operation and used when producing its result.</param>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("query")]
        public async Task<IActionResult> QueryDatabase([FromQuery] string table, [FromQuery] int take = 100)
        {
            try
            {
                object result = table.ToLower() switch
                {
                    "regexpatterns" => await db.RegexPatterns.Take(take).ToListAsync().ConfigureAwait(false),
                    "prompts" => await db.Prompts.Take(take).ToListAsync().ConfigureAwait(false),
                    "systemvariables" => await db.SystemVariables.Take(take).ToListAsync().ConfigureAwait(false),
                    "conversations" => await db.Conversations.Take(take).ToListAsync().ConfigureAwait(false),
                    "messages" => await db.Messages.Take(take).ToListAsync().ConfigureAwait(false),
                    "applicationlogs" => await db.ApplicationLogs.Take(take).ToListAsync().ConfigureAwait(false),
                    "councilknowledgeentries" => await db.CouncilKnowledgeEntries.Take(take).ToListAsync().ConfigureAwait(false),
                    "nativecommandlogs" => await db.NativeCommandLogs.Take(take).ToListAsync().ConfigureAwait(false),
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

        /// <summary>
        /// Lists configs for the database query API operation, delegating application logic to the controller's services and returning the resulting HTTP-facing value.
        /// </summary>
        /// <returns>The HTTP-facing result produced for the caller.</returns>
        [HttpGet("configs")]
        public async Task<IActionResult> ListConfigs()
        {
            try
            {
                var result = new Dictionary<string, object>
        {
            { "RegexPatterns", await db.RegexPatterns.ToListAsync<RegexPattern>().ConfigureAwait(false) },
            { "Prompts", await db.Prompts.Where(p => p.Language == "en").ToListAsync<PromptConfig>().ConfigureAwait(false) },
            { "SystemVariables", await db.SystemVariables.ToListAsync<SystemVariable>().ConfigureAwait(false) },
               { "CouncilKnowledgeEntries", await db.CouncilKnowledgeEntries.ToListAsync<CouncilKnowledgeEntry>().ConfigureAwait(false) }
        };
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListConfigs ex {ex.ToString()}");
                return BadRequest(new { error = ex.Message });
            }

        }
    }
        //[HttpGet("configs/{id}")]
        //public async Task<IActionResult> GetConfigEntry(string id)
        //{
        //    try
        //    {
        //        var entity = await db.Set<IClaimsIdentity>(id).FirstOrDefaultAsync();
        //        if (entity == null)
        //            return NotFound($"No config entry found with ID '{id}'");

        //        // Return appropriate data based on type
        //        switch (entity.Type)
        //        {
        //            case "RegexPattern":
        //                return Ok(new Regex(entity.Pattern, entity.Flags));
        //            case "PromptConfig":
        //                return Ok(entity.Text);
        //            default:
        //                return Ok(ParseValue<T>(entity.ValueString, entity.DataType));
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.LogError(ex, $"Error in GetConfigEntry id {id.ToString()} ex {ex.ToString()}");
        //        return BadRequest(new { error = ex.Message });
        //    }
          
        //}

        //private static T ParseValue<T>(string valueString, string dataType) => ... // Implementation omitted

}
