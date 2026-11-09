using System.Globalization;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services.ConfigurationSteward;

/// <summary>
/// Checks whether Council steps without a saved tool preference attach every registered native tool. With hundreds of
/// tool schemas the prompt outgrows small context windows and the provider cuts the oldest content, including tools.
/// </summary>
/// <param name="dbContextFactory">LocalGPT database read for the stored mode row.</param>
/// <param name="functionRegistry">Registered DXFunctions counted as native tool candidates.</param>
/// <param name="logger">Logger for check diagnostics.</param>
public sealed class NativeToolModeHealthCheck(
    IDbContextFactory<LocalGptMemoryDbContext> dbContextFactory,
    IDxAiFunctionRegistry functionRegistry,
    ILogger<NativeToolModeHealthCheck> logger) : IConfigurationHealthCheck
{
    /// <summary>Runtime value selecting how steps without a saved tool preference attach native tools.</summary>
    private const string ModeKey = "CouncilUnlistedStepNativeToolMode";

    /// <inheritdoc />
    public string CheckKey => "native-tool-mode";

    /// <inheritdoc />
    public string Category => "tools";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationFinding>> InspectAsync(ConfigurationStewardPolicy policy, CancellationToken cancellationToken = default)
    {
        try
        {
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var dbDisposal = db.ConfigureAwait(false);
            var row = await db.SystemVariables.AsNoTracking().SingleOrDefaultAsync(item => item.Name == ModeKey, cancellationToken).ConfigureAwait(false);
            if (row is null || !string.Equals(row.ValueString?.Trim(), "AllPolicyApproved", StringComparison.OrdinalIgnoreCase))
                return [];
            var nativeCandidates = functionRegistry.GetFunctions().Count(function => function.AvailableToAi && function.SupportsDirectInvocation &&
                (function.SupportsAutomaticInvocation || function.RequiresHumanConfirmation || function.SupportsDeferredApprovalRequest));
            if (nativeCandidates <= policy.MaximumNativeToolsWithoutPreference)
                return [];
            logger.LogInformation("Configuration check {CheckKey}: steps without a tool preference attach all {Count} native tools.", CheckKey, nativeCandidates);
            return
            [
                new ConfigurationFinding
                {
                    Key = CheckKey,
                    CheckKey = CheckKey,
                    Category = Category,
                    Severity = "Warning",
                    Title = $"Steps without a tool preference attach all {nativeCandidates} native tools",
                    Summary = "Every tool schema is part of the prompt. With this many tools small context windows overflow, the provider cuts the oldest content and requested tools (for example the PublisherStudio proposal function) disappear.",
                    VerificationNote = "Some workflows may rely on seeing every tool directly. In compact mode those tools stay reachable through dxai.tools.discover/preview/invoke and through the functions of requested capabilities.",
                    RecommendedOptionKey = "compact",
                    Evidence =
                    [
                        new() { Source = $"SystemVariables.{ModeKey}", ObservedValue = row.ValueString ?? string.Empty, Reference = "CompactWithBroker (shipped default)" },
                        new() { Source = "Registered native tool candidates", ObservedValue = nativeCandidates.ToString(CultureInfo.InvariantCulture), Reference = $"report threshold {policy.MaximumNativeToolsWithoutPreference}" }
                    ],
                    Options =
                    [
                        new()
                        {
                            Key = "compact",
                            Label = "Use the compact tool set",
                            Description = "Attach base tools, the functions of requested capabilities and the tool broker.",
                            Changes = [new() { TargetKind = ConfigurationRepairChange.SystemVariableKind, TargetKey = ModeKey, ExpectedCurrentValue = row.ValueString ?? string.Empty, NewValue = "CompactWithBroker" }]
                        },
                        new() { Key = "keep", Label = "Keep attaching all tools", Description = "Nothing changes." }
                    ]
                }
            ];
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Configuration check {CheckKey} was canceled.", CheckKey);
            else
                logger.LogError(exception, "Configuration check {CheckKey} failed.", CheckKey);
            throw;
        }
    }
}
