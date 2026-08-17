using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Coordinates database initialization behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class DatabaseInitializationService
{
    /// <summary>
    /// Performs seed regex as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedRegexAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
    {
            var existingNames = await db.RegexPatterns
                .Select(item => item.Name)
                .ToListAsync(token)
                .ConfigureAwait(false);

            var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in catalog.RegexPatterns)
            {
                // Add returns false for both database-existing names and duplicate
                // names encountered earlier in this same in-memory seed run.
                if (!existing.Add(item.Name))
                    continue;

                db.RegexPatterns.Add(new RegexPattern
                {
                    Name = item.Name,
                    Pattern = item.Pattern,
                    Flags = item.Flags,
                    CreatedOn = DateTime.UtcNow,
                    UpdatedOn = DateTime.UtcNow
                });
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedRegexAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedRegexAsync)} failed.");
        throw;
    }
}
    /// <summary>
    /// Performs seed prompts as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedPromptsAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
    {
            var existing = await db.Prompts.Select(x => new { x.Key, x.Language }).ToListAsync(token).ConfigureAwait(false);
            foreach (var item in catalog.Prompts)
            {
                if (existing.Any(x =>
                        string.Equals(x.Key, item.Key, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.Language, item.Language, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                db.Prompts.Add(new PromptConfig
                {
                    Key = item.Key,
                    Language = item.Language,
                    Text = item.Text,
                    LastUpdated = DateTime.UtcNow
                });
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedPromptsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedPromptsAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs seed variables as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="db">Database value supplied to the database initialization operation and used when producing its result.</param>
    /// <param name="token">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SeedVariablesAsync(LocalGptMemoryDbContext db, CancellationToken token)
    {
    try
    {
            var existingRows = await db.SystemVariables.ToListAsync(token).ConfigureAwait(false);
            var existing = existingRows.ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
            foreach (var item in catalog.Variables)
            {
                if (!existing.TryGetValue(item.Name, out var row))
                {
                    row = new SystemVariable
                    {
                        Name = item.Name,
                        ValueString = item.Value,
                        DataType = item.DataType,
                        LastUpdated = DateTime.UtcNow
                    };

                    // Make the newly queued row visible to later entries in this same seed run.
                    existing.Add(item.Name, row);
                    db.SystemVariables.Add(row);
                    continue;
                }

                if (item.DataType.Equals(typeof(string[]).FullName, StringComparison.Ordinal)
                    && !IsJsonStringArray(row.ValueString))
                {
                    row.ValueString = item.Value;
                    row.DataType = item.DataType;
                    row.LastUpdated = DateTime.UtcNow;
                    logger.LogWarning(
                        "Repaired legacy runtime-policy collection {RuntimePolicyCollectionName} with its canonical JSON seed value.",
                        item.Name);
                    continue;
                }

                // Lossless default evolution: only replace values that exactly match a previous built-in default.
                // Any user-edited value remains authoritative.
                if (item.Name.Equals("DefaultContextTokens", StringComparison.OrdinalIgnoreCase)
                    && row.ValueString == "65536"
                    && item.Value == "262144")
                {
                    row.ValueString = item.Value;
                    row.DataType = item.DataType;
                    row.LastUpdated = DateTime.UtcNow;
                }

                if (item.Name.Equals(nameof(LocalGptRuntimeCollection.KnowledgeFiles), StringComparison.OrdinalIgnoreCase)
                    && IsPreviousKnowledgeFilesDefault(row.ValueString)
                    && !string.Equals(row.ValueString, item.Value, StringComparison.Ordinal))
                {
                    row.ValueString = item.Value;
                    row.DataType = item.DataType;
                    row.LastUpdated = DateTime.UtcNow;
                    logger.LogInformation(
                        "Upgraded the untouched built-in KnowledgeFiles collection with the LocalGPT AI-provider bootstrap reference article.");
                }
            }
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedVariablesAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(DatabaseInitializationService)}.{nameof(SeedVariablesAsync)} failed.");
        throw;
    }
}

    /// <summary>Determines whether a persisted KnowledgeFiles collection is exactly the pre-3.0.5 built-in default so seed evolution never overwrites user edits.</summary>
    /// <param name="value">Persisted JSON collection to compare with the previous built-in default.</param>
    /// <returns><see langword="true"/> only when the persisted ordered collection exactly matches the pre-3.0.5 default.</returns>
    private bool IsPreviousKnowledgeFilesDefault(string? value)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var values = JsonSerializer.Deserialize<string[]>(value);
            if (values is null)
                return false;
            string[] previousDefault =
            [
                "AGENTS.md", "SECURITY.md", "docs/index.md", "docs/architecture/system-overview.md",
                "docs/architecture/ai-host.md", "docs/architecture/council-runtime.md",
                "docs/architecture/project-data.md", "docs/architecture/onewire-security.md",
                "docs/architecture/frontend-and-themes.md", "docs/engineering/build-validation.md",
                "docs/reference/capability-map.md", "docs/reference/toolchain-discovery.md",
                "docs/reference/design-evolution.md", "docs/guide/embedded-and-games.md",
                "docs/COUNCIL_KNOWLEDGE_SEED.sql"
            ];
            return values.SequenceEqual(previousDefault, StringComparer.Ordinal);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Could not compare the persisted KnowledgeFiles collection with the previous built-in default; user data will remain untouched.");
            return false;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Comparing the persisted KnowledgeFiles collection with the previous built-in default failed.");
            throw;
        }
    }

    /// <summary>
    /// Determines whether JSON string array as part of the database initialization service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="value">Value value supplied to the database initialization operation and used when producing its result.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool IsJsonStringArray(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind == JsonValueKind.Array
                && document.RootElement.EnumerateArray().All(item => item.ValueKind == JsonValueKind.String);
        }
        catch (JsonException exception)
        {
            logger.LogDebug(
                exception,
                "Runtime-policy collection candidate is not a JSON string array and requires compatibility repair.");
            return false;
        }
    }

}
