using DevExpress.Xpo;
using global::LocalGPT.BusinessObjects;
using global::LocalGPT.BusinessObjects.EFCore;
using Microsoft.AspNetCore.Mvc;

namespace LocalGPT.Controller
{
 
    [ApiController]
    [Route("__diag/knowledge/[controller]")]
    public class DatabaseKnowledgeController(LocalGptMemoryDbContext db, ILogger<DatabaseKnowledgeController> logger) : ControllerBase
    {

        // GET /__diag/knowledge/database-configs?take=100
        [HttpGet("configs")]
        public async Task<IActionResult> ListConfigs([FromQuery] int take = 100)
        {
            try
            {
                var result = new Dictionary<string, object>
        {
            { "RegexPatterns", await db.RegexPatterns.Take(take).ToListAsync<RegexPattern>().ConfigureAwait(false) },
            { "Prompts",       await db.Prompts.Take(take).ToListAsync<PromptConfig>().ConfigureAwait(false) },
            { "SystemVariables", await db.SystemVariables.Take(take).ToListAsync<SystemVariable>().ConfigureAwait(false) }
        };
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ListConfigs take {take} ex {ex.ToString()}");
                return new BadRequestResult();
            }
        }

        // GET /__diag/knowledge/database-configs/{id}
        [HttpGet("configs/{id:int}")]
        public async Task<IActionResult> GetConfigById(int id)
        {
            try
            {
                var entity = await db.Set<object>().FindAsync(id).ConfigureAwait(false);
                if (entity == null) return NotFound($"No entry with Id {id}");
                return Ok(entity);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"GetConfigById id {id} ex {ex.ToString()}");
                return new BadRequestResult();
            }
        }
    }
    
}