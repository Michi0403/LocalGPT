using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>
/// Exposes the persisted DXAIFunction that resolves the LocalGPT Minecraft datapack format metadata for a requested Minecraft version.
/// </summary>
/// <param name="json">DXAIFunction JSON binding and result service.</param>
/// <param name="datapacks">Minecraft datapack domain service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ResolveMinecraftDatapackVersionFunction(
    IDxAiFunctionJsonService json,
    MinecraftDatapackService datapacks,
    ILogger<ResolveMinecraftDatapackVersionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable descriptor discovered by the DXAIFunction registry and persisted by the catalog synchronization service.</summary>
    /// <value>The stable Minecraft datapack DXAIFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "minecraft.datapack.version.resolve",
        "POST",
        "/api/dxai/functions/minecraft.datapack.version.resolve/invoke",
        "Resolve LocalGPT's curated datapack pack-format and function-folder metadata for a Minecraft Java version.",
        "JSON parameters: minecraftVersion optional; when omitted LocalGPT uses its configured default Minecraft version.",
        "Read-only version metadata. Unknown versions are returned as cautious fallbacks that explicitly require verification against the official Minecraft version manifest.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"minecraftVersion":{"type":["string","null"],"maxLength":64}},"additionalProperties":false}""");

    /// <summary>Resolves datapack metadata through the Minecraft datapack domain service.</summary>
    /// <param name="request">DXAIFunction invocation request.</param>
    /// <param name="cancellationToken">Cancellation token for the invocation.</param>
    /// <returns>The DXAIFunction invocation result.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<MinecraftDatapackVersionParameters>(request.Parameters);
            if (!binding.Succeeded)
                return Task.FromResult(json.InvalidParameters(binding.Error));

            var result = datapacks.MinecraftDatapackVersionInfoResolve(binding.Value.MinecraftVersion, logger);
            logger.LogDebug("DXAIFunction resolved Minecraft datapack metadata for requested version {MinecraftVersion}.", binding.Value.MinecraftVersion);
            return Task.FromResult(json.Success(result));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Minecraft datapack version DXAIFunction was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Minecraft datapack version DXAIFunction failed; parameters were omitted from logs.");
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Minecraft datapack metadata could not be resolved. Review LocalGPT logs." });
        }
    }

    /// <summary>Contains the optional Minecraft version supplied to the datapack-version resolver.</summary>
    private sealed class MinecraftDatapackVersionParameters
    {
        /// <summary>Initializes an empty parameter object for JSON binding.</summary>
        public MinecraftDatapackVersionParameters() { }

        /// <summary>Carries the Minecraft Java version that the DXAIFunction forwards into the curated version resolver.</summary>
        /// <value>The requested Minecraft Java version, or <see langword="null"/> to use the configured default.</value>
        public string? MinecraftVersion { get; set; }
    }
}

/// <summary>
/// Exposes the persisted DXAIFunction that resolves LocalGPT's curated Minecraft loader and dependency-version metadata.
/// </summary>
/// <param name="json">DXAIFunction JSON binding and result service.</param>
/// <param name="projects">Minecraft project domain service.</param>
/// <param name="logger">Logger used for operational diagnostics.</param>
public sealed class ResolveMinecraftDependencyVersionFunction(
    IDxAiFunctionJsonService json,
    MinecraftProjectService projects,
    ILogger<ResolveMinecraftDependencyVersionFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Gets the stable descriptor discovered by the DXAIFunction registry and persisted by the catalog synchronization service.</summary>
    /// <value>The stable Minecraft dependency DXAIFunction descriptor.</value>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "minecraft.dependency.version.resolve",
        "POST",
        "/api/dxai/functions/minecraft.dependency.version.resolve/invoke",
        "Resolve LocalGPT's curated Minecraft Java, Gradle, loader/API and datapack dependency metadata before generating a Minecraft project.",
        "JSON parameters: loader and minecraftVersion optional; javaVersion and gradleVersion optional overrides.",
        "Read-only dependency metadata. Fallback values explicitly report NeedsVerification and do not authorize downloads, builds, or filesystem changes.",
        IsReadOnly: true,
        AvailableToAi: true,
        SupportsDirectInvocation: true,
        SupportsAutomaticInvocation: true,
        Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"loader":{"type":["string","null"],"maxLength":64},"minecraftVersion":{"type":["string","null"],"maxLength":64},"javaVersion":{"type":["string","null"],"maxLength":64},"gradleVersion":{"type":["string","null"],"maxLength":64}},"additionalProperties":false}""");

    /// <summary>Resolves dependency metadata through the Minecraft project domain service.</summary>
    /// <param name="request">DXAIFunction invocation request.</param>
    /// <param name="cancellationToken">Cancellation token for the invocation.</param>
    /// <returns>The DXAIFunction invocation result.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<MinecraftDependencyVersionParameters>(request.Parameters);
            if (!binding.Succeeded)
                return Task.FromResult(json.InvalidParameters(binding.Error));

            var parameters = binding.Value;
            var result = projects.ResolveMinecraftDependencyVersionInfo(
                parameters.Loader,
                parameters.MinecraftVersion,
                logger,
                parameters.JavaVersion,
                parameters.GradleVersion);
            logger.LogDebug("DXAIFunction resolved Minecraft dependency metadata for loader {Loader} and requested version {MinecraftVersion}.", parameters.Loader, parameters.MinecraftVersion);
            return Task.FromResult(json.Success(result));
        }
        catch (OperationCanceledException exception)
        {
            logger.LogDebug(exception, "Minecraft dependency version DXAIFunction was cancelled.");
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Minecraft dependency version DXAIFunction failed; parameters were omitted from logs.");
            return Task.FromResult(new DxAiFunctionInvocationResult { Succeeded = false, Status = "Failed", Error = "Minecraft dependency metadata could not be resolved. Review LocalGPT logs." });
        }
    }

    /// <summary>Contains the optional Minecraft loader and dependency-version inputs supplied to the dependency resolver.</summary>
    private sealed class MinecraftDependencyVersionParameters
    {
        /// <summary>Initializes an empty parameter object for JSON binding.</summary>
        public MinecraftDependencyVersionParameters() { }

        /// <summary>Carries the loader family that selects the curated Fabric, NeoForge, Paper, or Datapack dependency mapping.</summary>
        /// <value>The requested loader, or <see langword="null"/> to use service defaults.</value>
        public string? Loader { get; set; }

        /// <summary>Carries the Minecraft Java version that the DXAIFunction forwards into the curated version resolver.</summary>
        /// <value>The requested Minecraft Java version, or <see langword="null"/> to use the configured default.</value>
        public string? MinecraftVersion { get; set; }

        /// <summary>Carries an optional caller override for the Java runtime version returned with the dependency mapping.</summary>
        /// <value>The Java version override, or <see langword="null"/> to use the curated mapping.</value>
        public string? JavaVersion { get; set; }

        /// <summary>Carries an optional caller override for the Gradle version returned with the dependency mapping.</summary>
        /// <value>The Gradle version override, or <see langword="null"/> to use the configured default.</value>
        public string? GradleVersion { get; set; }
    }
}
