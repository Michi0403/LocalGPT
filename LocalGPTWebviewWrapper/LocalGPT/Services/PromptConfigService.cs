using DevExpress.CodeParser;
using DevExpress.Office.NumberConverters;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.BusinessObjects.Models;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using static System.Net.WebRequestMethods;

namespace LocalGPT.Services
{
    public class PromptConfigService(LocalGptMemoryDbContext db, ILogger logger) : IPromptConfigService
    {

        public async Task<string> GetPromptAsync(string key, string language = "en")
        {
            try
            {
                var p = await db.Prompts.FindAsync(key, language);
                return p?.Text ?? throw new KeyNotFoundException($"Prompt '{key}' in '{language}' not found");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetPromptAsync key {key} language {language}");
                return string.Empty;
            }
     
        }

        public async Task UpdatePromptAsync(PromptConfigDto dto)
        {
            try
            {
                var entity = await db.Prompts.FindAsync(dto.Key, dto.Language);
                if (entity == null)
                    await db.Prompts.AddAsync(new PromptConfig
                    {
                        Key = dto.Key,
                        Language = dto.Language,
                        Text = dto.Text,
                        LastUpdated = DateTime.UtcNow
                    });
                else
                {
                    entity.Text = dto.Text;
                    entity.LastUpdated = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetPromptAsync dto {dto.ToString()}");
            }
        }

        public async Task<IEnumerable<PromptConfig>> ListPromptsAsync(string? language = null)
        {
            try
            {
                return language == null
             ? await db.Prompts.ToListAsync()
             : await db.Prompts.Where(p => p.Language == language).ToListAsync<PromptConfig>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ListPromptsAsync language {language.ToString()}");
                return await Task.FromResult<IEnumerable<PromptConfig>>(
               Enumerable.Empty<PromptConfig>()
           );
            }
         
        }


        public async Task<string> GetPromptAsync(PromptConfigDto dto)
        {
            try
            {
                var prompt = await db.Prompts.FirstAsync(p => p.Language == dto.Language && dto.Key == dto.Key);
                return prompt.Text;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetPromptAsync dto {dto.ToString()}");
                return string.Empty;
           }
        }
    }
}
