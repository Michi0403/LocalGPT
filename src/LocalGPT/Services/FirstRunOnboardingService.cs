using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Builds LocalGPT's first-run setup guide from database-owned teams and presets plus optional loopback model discovery.
/// </summary>
/// <param name="variables">Stores the persisted onboarding-completion flag.</param>
/// <param name="systemVariables">Provides the maintained variable key instead of a string literal.</param>
/// <param name="connectivityProbe">Discovers supported local AI providers without downloading or starting models.</param>
/// <param name="teamConfigurations">Reads seeded and user-owned council teams.</param>
/// <param name="modelPresets">Reads seeded and user-owned model presets.</param>
/// <param name="version">Provides the running LocalGPT version.</param>
/// <param name="logger">Writes bounded operational diagnostics.</param>
[DocumentationUpdated("2.1.20")]
public sealed class FirstRunOnboardingService(
    IVariableStoreService variables,
    ISystemVariableDefinitionService systemVariables,
    IAiConnectivityProbe connectivityProbe,
    ICouncilTeamConfigurationService teamConfigurations,
    IModelPresetService modelPresets,
    ICustomVersion version,
    ILogger<FirstRunOnboardingService> logger) : IFirstRunOnboardingService
{
    /// <inheritdoc />
    public async Task<FirstRunOnboardingStatus> GetStatusAsync(bool refreshConnectivity, CancellationToken cancellationToken = default)
    {
        var status = new FirstRunOnboardingStatus
        {
            Version = version.Version,
            IsCompleted = await ReadCompletionAsync(cancellationToken).ConfigureAwait(false),
            InstallerProfiles = CreateInstallerProfiles().ToList(),
            QuickStarts = CreateQuickStarts().ToList()
        };

        try
        {
            status.CouncilTeamKeys = (await teamConfigurations.GetTeamsAsync(includeDisabled: false, cancellationToken).ConfigureAwait(false))
                .Select(team => team.Key)
                .OrderBy(key => key, StringComparer.OrdinalIgnoreCase)
                .ToList();
            status.ModelPresetNames = (await modelPresets.GetPresetsAsync(includeArchived: false, cancellationToken).ConfigureAwait(false))
                .Select(preset => preset.Name)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Onboarding could not read the current council team or model preset catalog.");
            status.Warnings.Add("Council teams or model presets could not be inspected. The setup links remain available.");
        }

        if (!refreshConnectivity)
            return status;

        try
        {
            var hosts = await connectivityProbe.DiscoverLocalHostsAsync(cancellationToken).ConfigureAwait(false);
            status.LocalAiReachable = hosts.Any(host => host.IsReachable);
            status.InstalledModels = hosts
                .Where(host => host.IsReachable)
                .SelectMany(host => host.Models)
                .Select(model => model.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (!status.LocalAiReachable)
                status.Warnings.Add("No supported loopback AI provider answered. Open Install before starting a council run.");
            else if (status.InstalledModels.Count == 0)
                status.Warnings.Add("A local provider answered, but it reported no installed models.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogInformation(exception, "Optional first-run local AI discovery did not complete.");
            status.Warnings.Add("Local AI discovery did not complete. Use Install to refresh connectivity manually.");
        }

        return status;
    }

    /// <inheritdoc />
    public async Task CompleteAsync(bool userConfirmed, CancellationToken cancellationToken = default)
    {
        if (!userConfirmed)
            throw new InvalidOperationException("Explicit user confirmation is required before the first-run guide is dismissed.");
        await variables.SetAsync(systemVariables.FirstRunOnboardingCompleted.Name, true, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The LocalGPT first-run onboarding guide was marked completed.");
    }

    /// <summary>
    /// Reads the database-owned onboarding-completion flag and treats an absent legacy value as incomplete.
    /// </summary>
    /// <param name="cancellationToken">Cancels the asynchronous read.</param>
    /// <returns>A task that completes with true when onboarding was previously completed.</returns>
    private async Task<bool> ReadCompletionAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await variables.GetAsync<bool>(systemVariables.FirstRunOnboardingCompleted.Name, cancellationToken).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates the maintained installer profiles exposed by UI, controller and DXFunction surfaces.
    /// </summary>
    /// <returns>The immutable installer-profile catalog.</returns>
    private IReadOnlyList<OnboardingInstallerProfile> CreateInstallerProfiles() =>
    [
        new(
            "game-low-b",
            "Fast game council",
            "Small models for low-latency GameDirector, creature and reactive-object council roles.",
            "LocalGPTInstallerConsole.exe --install-ollama --pull-models --range RTX3060",
            ["qwen3.5:0.8b", "qwen3.5:2b", "llama3.2:1b", "codegemma:2b"],
            ["Michi0403/LocalGPT"]),
        new(
            "development",
            "Development and code-curation council",
            "Coder and reviewer models plus official .NET, PowerShell, Minecraft, Arduino and ESP documentation sources.",
            "LocalGPTInstallerConsole.exe --pull-models --range Full --setup-learning-base --import-recommended",
            ["qwen3-coder:30b", "deepseek-coder-v2:16b", "deepseek-coder:6.7b", "codegemma:7b", "qwen3:8b"],
            ["dotnet/docs", "MicrosoftDocs/microsoftgraph-docs-powershell", "Mojang/bedrock-samples", "arduino/docs-content", "espressif/esp-idf", "platformio/platformio-core"]),
        new(
            "knowledge-only",
            "Official documentation learning base",
            "Downloads approved repository sources and manifests without pulling additional models.",
            "LocalGPTInstallerConsole.exe --setup-learning-base --import-recommended",
            [],
            ["dotnet/docs", "MicrosoftDocs/windows-dev-docs", "Mojang/bedrock-protocol-docs", "arduino/reference-en", "espressif/arduino-esp32"])
    ];

    /// <summary>
    /// Creates direct Chat routes for the maintained seeded teams and model presets.
    /// </summary>
    /// <returns>The immutable quick-start catalog.</returns>
    private IReadOnlyList<CouncilQuickStart> CreateQuickStarts() =>
    [
        QuickStart("benchmark", "Benchmark installed models", "Runs the bounded benchmark council and lets code-curator roles compare installed Ollama models.", "adaptive-model-benchmark", "Benchmark Candidate Pool", "benchmark-council-start"),
        QuickStart("game", "Fast GameDirector council", "Uses small models while the deterministic GameDirector remains authoritative.", "game-director-runtime", "Fast Game Council (Low-B)", "game-director-council-start"),
        QuickStart("csharp", "Modern C# host team", "Plans and verifies a hosted .NET solution through architecture, implementation, regex, build and curator rounds.", "csharp-modern-host-development", "Code Curator Council", "csharp-host-council-start"),
        QuickStart("powershell", "PowerShell build-system team", "Uses LocalGPT's repository-validation round order as the reference workflow.", "powershell-build-development", "Code Curator Council", "powershell-build-council-start"),
        QuickStart("java", "Java hosted application team", "Builds Maven or Gradle services with bounded project and compiler policies.", "java-hosted-development", "Code Curator Council", "java-hosted-council-start"),
        QuickStart("minecraft", "Minecraft development team", "Routes datapack, scripting and Java-mod work to separate roles and verification rounds.", "minecraft-development", "Code Curator Council", "minecraft-development-council-start")
    ];

    /// <summary>
    /// Creates one URL-encoded Chat quick start.
    /// </summary>
    /// <param name="key">Stable quick-start key.</param>
    /// <param name="displayName">Human-readable quick-start label.</param>
    /// <param name="description">Short workload description.</param>
    /// <param name="teamKey">Seeded council-team key.</param>
    /// <param name="presetName">Seeded model-preset name.</param>
    /// <param name="starterPromptKey">Stable prompt key submitted after the Council chat is ready.</param>
    /// <returns>The configured quick-start record.</returns>
    private CouncilQuickStart QuickStart(string key, string displayName, string description, string teamKey, string presetName, string starterPromptKey) =>
        new(key, displayName, description, teamKey, presetName, starterPromptKey,
            $"/chat?team={Uri.EscapeDataString(teamKey)}&preset={Uri.EscapeDataString(presetName)}&starter={Uri.EscapeDataString(starterPromptKey)}&autoStartCouncil=true&newCouncil=true");
}
