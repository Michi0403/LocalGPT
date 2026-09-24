using LocalGPT.BusinessObjects;
using System.Text.Json;

namespace LocalGPT.Services;

/// <summary>
/// Coordinates local AI runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
/// </summary>
public sealed partial class LocalAiRuntimeService
{
    /// <summary>
    /// Represents the LocalGPT-managed virtual environment and maintained Whisper adapter through
    /// the same database-backed toolchain model used by other runtimes. User-maintained profiles
    /// are authoritative and are never overwritten by this synchronization.
    /// </summary>
    /// <param name="whisperVariant">Whisper variant value supplied to the local AI runtime operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task EnsureManagedPythonToolchainAsync(string? whisperVariant, CancellationToken cancellationToken)
    {
        try
        {
            var config = CurrentConfig();
            var environmentPath = ResolveEnvironmentPath(config);
            var environmentPython = ResolveEnvironmentPython(environmentPath);
            if (!File.Exists(environmentPython))
                return;

            var installations = await projectMaintenance.GetCompilerInstallationsAsync(cancellationToken).ConfigureAwait(false);
            var existing = installations.FirstOrDefault(item =>
                item.Language.Equals("Python", StringComparison.OrdinalIgnoreCase) &&
                platform.PathComparer.Equals(Path.GetFullPath(item.ExecutablePath), Path.GetFullPath(environmentPython)));
            var basePython = installations.FirstOrDefault(item =>
                item.Language.Equals("Python", StringComparison.OrdinalIgnoreCase) && item.IsDefaultForLanguage);

            var installation = await projectMaintenance.SaveCompilerInstallationAsync(new SaveProjectCompilerInstallationRequest
            {
                Id = existing?.Id,
                Name = "LocalGPT managed Python environment",
                Language = "Python",
                ExecutablePath = environmentPython,
                CompilerHomePath = environmentPath,
                Version = existing?.Version ?? basePython?.Version ?? string.Empty,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
                DiscoverySource = "LocalGPT managed environment",
                ToolchainKind = "runtime",
                DetectedPlatform = platform.ProviderBootstrapToken,
                ValidationArguments = "--version",
                KnowledgeProfileKey = "python",
                IsEnabled = true,
                IsDefaultForLanguage = false,
                UserConfirmed = true
            }, cancellationToken).ConfigureAwait(false);

            var currentProfile = await toolchainProfiles.GetProfileAsync("speech.whisper", cancellationToken).ConfigureAwait(false);
            if (currentProfile is not null &&
                !currentProfile.UpdatedBy.Equals("Local AI runtime service", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Preserved the user-maintained speech.whisper toolchain profile while synchronizing the managed Python environment.");
                return;
            }

            var variant = NormalizeWhisperVariant(whisperVariant);
            var installedWhisper = GetInstalledModels()
                .Where(item => item.Adapter.Equals("openai-whisper", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.RuntimeModelName.Equals(variant, StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(item => item.InstalledAtUtc)
                .FirstOrDefault();
            var bridgePath = Path.Combine(environment.ContentRootPath, "Runtime", "Python", "localgpt_runtime_bridge.py");
            var profile = currentProfile ?? new ToolchainExecutionProfile
            {
                ProfileKey = "speech.whisper",
                CreatedAtUtc = DateTime.UtcNow
            };
            profile.Name = "OpenAI Whisper transcription";
            profile.ToolchainInstallationId = installation.Id;
            profile.CapabilityKey = "speech.transcribe";
            profile.ExecutionKind = ToolchainExecutionKind.Script;
            profile.EntryPoint = bridgePath;
            profile.ArgumentsJson = "[]";
            profile.WorkingDirectory = environmentPath;
            profile.IsEnabled = true;
            profile.IsDefaultForCapability = true;
            // Approval is owned by the DX function/dispatcher. The profile itself is an internal
            // capability binding, so it must not demand a second nested approval.
            profile.RequiresApproval = false;
            profile.ConfigurationJson = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["model"] = variant,
                ["modelPath"] = installedWhisper?.LocalPath ?? string.Empty,
                ["task"] = NormalizeSpeechTask(config.DefaultSpeechTask),
                ["device"] = NormalizeRuntimeDevice(config.DefaultDevice),
                ["runtime"] = "LocalGPT managed Python environment",
                ["adapter"] = "openai-whisper",
                ["capabilityAdapter"] = "Runtime/Python/localgpt_runtime_bridge.py"
            }, JsonOptions);
            profile.UpdatedBy = "Local AI runtime service";
            await toolchainProfiles.SaveProfileAsync(new SaveToolchainExecutionProfileRequest
            {
                UserConfirmed = true,
                Profile = profile
            }, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Synchronized the maintained Whisper capability with the generic managed-Python toolchain profile; paths and model identity were omitted from logs.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError("Synchronizing the managed Python/Whisper toolchain profile failed with {ExceptionType}; paths and model identity were omitted from logs.", exception.GetType().Name);
            throw;
        }
    }
}
