using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Text.Json;

namespace LocalGPT.Services.Persistence;

/// <summary>
/// Owns the deterministic first-run feed for LocalGPT runtime-policy records.
/// The values are inserted only when their database rows do not already exist.
/// Runtime consumers never read these literals directly; they use
/// <see cref="ILocalGptRuntimePolicyStoreService"/>.
/// </summary>
public sealed class LocalGptRuntimePolicySeedDataService : ILocalGptRuntimePolicySeedDataService
{
    private readonly LocalGptRuntimePolicySeedModel seed;
    private readonly ILogger<LocalGptRuntimePolicySeedDataService> logger;

    public LocalGptRuntimePolicySeedDataService(
        ISystemVariableDefinitionService systemVariables,
        ILogger<LocalGptRuntimePolicySeedDataService> logger)
    {
        this.logger = logger;
        try
        {
            const string coreProjectVariableName = "LocalGptCoreProjectId";
            const string executableVariableName = "AllowedNativeExecutablesJson";
            const string inlineRegexName = "runtime.native.powershell-inline-command";
            const string fileRegexName = "runtime.native.powershell-file";
            const string sensitiveRegexName = "runtime.native.sensitive-argument";
            var coreProjectId = Guid.Parse("7f4d7b4a-b622-4d15-8e44-9dfae2aa6101");
            var executableValues = new[]
            {
                "powershell.exe",
                "pwsh.exe",
                "gradle",
                "gradle.bat",
                "gradlew",
                "gradlew.bat",
                "java",
                "java.exe"
            };

            seed = new LocalGptRuntimePolicySeedModel
            {
                LocalGptCoreProjectIdVariableName = coreProjectVariableName,
                AllowedNativeExecutablesVariableName = executableVariableName,
                RegexTimeoutVariableName = systemVariables.RegexMatchTimeoutMilliseconds.Name,
                PowerShellInlineCommandRegexName = inlineRegexName,
                PowerShellFileRegexName = fileRegexName,
                SensitiveArgumentRegexName = sensitiveRegexName,
                LocalGptCoreProjectId = coreProjectId,
                SystemVariables =
                [
                    new LocalGptRuntimeSystemVariableSeed(
                        coreProjectVariableName,
                        coreProjectId.ToString("D"),
                        typeof(Guid).FullName ?? nameof(Guid)),
                    new LocalGptRuntimeSystemVariableSeed(
                        executableVariableName,
                        JsonSerializer.Serialize(executableValues),
                        typeof(string[]).FullName ?? "System.String[]")
                ],
                RegexPatterns =
                [
                    new LocalGptRuntimeRegexSeed(
                        inlineRegexName,
                        @"(^|\s)-EncodedCommand(\s|$)|(^|\s)-Command(\s|$)|(^|\s)-c(\s|$)",
                        "i,c,compiled"),
                    new LocalGptRuntimeRegexSeed(
                        fileRegexName,
                        @"(^|\s)-File\s+(?:""(?<path>[^""]+)""|'(?<path>[^']+)'|(?<path>\S+))",
                        "i,c,compiled"),
                    new LocalGptRuntimeRegexSeed(
                        sensitiveRegexName,
                        @"(?<name>--?(?:api[-_]?key|key|token|secret|password|passwd|pwd))(?<separator>\s+|=)(?<value>""[^""]*""|'[^']*'|\S+)",
                        "i,c,compiled")
                ]
            };
            logger.LogInformation($"Prepared the LocalGPT runtime-policy first-run feed with {seed.SystemVariables.Count} system-variable records, {seed.RegexPatterns.Count} regex records and {executableValues.Length} native executable entries.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not prepare the LocalGPT runtime-policy first-run feed: {exception.Message}");
            throw;
        }
    }

    public LocalGptRuntimePolicySeedModel GetSeed()
    {
        try
        {
            logger.LogTrace($"Returned the LocalGPT runtime-policy seed model with {seed.SystemVariables.Count} system-variable records and {seed.RegexPatterns.Count} regex records.");
            return seed;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, $"Could not return the LocalGPT runtime-policy seed model: {exception.Message}");
            throw;
        }
    }
}
