namespace LocalGPT.BusinessObjects;

/// <summary>
/// Describes one installer profile that can prepare models or repository knowledge for a LocalGPT workstation.
/// </summary>
/// <param name="Key">Stable profile key used by UI and API clients.</param>
/// <param name="DisplayName">Human-readable profile name.</param>
/// <param name="Purpose">Short explanation of the profile's intended workload.</param>
/// <param name="Command">Reviewable installer command that the user can run explicitly.</param>
/// <param name="Models">Recommended Ollama model tags for the profile.</param>
/// <param name="Repositories">Recommended GitHub repositories for the profile's learning base.</param>
[DocumentationUpdated("2.1.20")]
public sealed record OnboardingInstallerProfile(
    string Key,
    string DisplayName,
    string Purpose,
    string Command,
    IReadOnlyList<string> Models,
    IReadOnlyList<string> Repositories)
{
    /// <summary>
    /// Gets the model summary value that forms part of the onboarding installer profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The model summary value exposed by <see cref="OnboardingInstallerProfile"/>.</value>
    public string ModelSummary => string.Join(", ", Models);

    /// <summary>
    /// Gets the repository summary value that forms part of the onboarding installer profile state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The repository summary value exposed by <see cref="OnboardingInstallerProfile"/>.</value>
    public string RepositorySummary => string.Join(", ", Repositories);
}

/// <summary>
/// Describes a direct route into a seeded LocalGPT team and model-preset combination.
/// </summary>
/// <param name="Key">Stable quick-start key.</param>
/// <param name="DisplayName">Human-readable quick-start label.</param>
/// <param name="Description">Explanation of the intended round and resource profile.</param>
/// <param name="CouncilTeamKey">Seeded council-team key selected by the route.</param>
/// <param name="ModelPresetName">Seeded model-preset name selected by the route.</param>
/// <param name="StarterPromptKey">Stable prompt key submitted by the direct Council starter.</param>
/// <param name="Route">Application-relative Chat route containing the requested team, preset and starter prompt.</param>
[DocumentationUpdated("2.1.22")]
public sealed record CouncilQuickStart(
    string Key,
    string DisplayName,
    string Description,
    string CouncilTeamKey,
    string ModelPresetName,
    string StarterPromptKey,
    string Route);

/// <summary>
/// Represents the bounded first-run setup state shown by LocalGPT before the user starts normal work.
/// </summary>
[DocumentationUpdated("2.1.20")]
public sealed class FirstRunOnboardingStatus
{
    /// <summary>
    /// Gets or sets the version value that forms part of the first run onboarding status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The version value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public string Version { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the user has dismissed the first-run guide.</summary>
    /// <value>The is completed value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets whether at least one supported local AI endpoint answered the bounded discovery probe.</summary>
    /// <value>The local AI reachable value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public bool LocalAiReachable { get; set; }

    /// <summary>
    /// Gets or sets the installed models collection maintained or exposed by this first run onboarding status instance for downstream processing.
    /// </summary>
    /// <value>The installed models value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<string> InstalledModels { get; set; } = [];

    /// <summary>Gets or sets the currently available seeded or user-owned council team keys.</summary>
    /// <value>The council team keys value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<string> CouncilTeamKeys { get; set; } = [];

    /// <summary>
    /// Gets or sets the model preset names collection maintained or exposed by this first run onboarding status instance for downstream processing.
    /// </summary>
    /// <value>The model preset names value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<string> ModelPresetNames { get; set; } = [];

    /// <summary>Gets or sets installer profiles that remain user-triggered and reviewable.</summary>
    /// <value>The installer profiles value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<OnboardingInstallerProfile> InstallerProfiles { get; set; } = [];

    /// <summary>Gets or sets direct Chat quick starts for common council workloads.</summary>
    /// <value>The quick starts value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<CouncilQuickStart> QuickStarts { get; set; } = [];

    /// <summary>
    /// Gets or sets the warnings collection maintained or exposed by this first run onboarding status instance for downstream processing.
    /// </summary>
    /// <value>The warnings value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Gets the installer route value that forms part of the first run onboarding status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The installer route value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public string InstallerRoute => "/install";

    /// <summary>
    /// Gets the council teams route value that forms part of the first run onboarding status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The council teams route value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public string CouncilTeamsRoute => "/council-teams";

    /// <summary>
    /// Gets the documentation route value that forms part of the first run onboarding status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The documentation route value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public string DocumentationRoute => "/help";

    /// <summary>
    /// Gets the chat route value that forms part of the first run onboarding status state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The chat route value exposed by <see cref="FirstRunOnboardingStatus"/>.</value>
    public string ChatRoute => "/chat";
}
