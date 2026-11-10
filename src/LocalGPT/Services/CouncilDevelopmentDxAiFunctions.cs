using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;

namespace LocalGPT.Services;

/// <summary>Previews the actual CodeDOM output for a staged project without editing or executing it.</summary>
/// <param name="json">DXFunction JSON binding and response service.</param>
/// <param name="codeDom">Existing shared reviewed CodeDOM emitter.</param>
/// <param name="artifacts">Application-owned Council workspace root.</param>
/// <param name="runtime">Workspace path boundary.</param>
/// <param name="logger">Operational diagnostics.</param>
public sealed class PreviewCouncilWorkspaceCodeDomFunction(
    IDxAiFunctionJsonService json,
    ICodeDomSourceService codeDom,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    ILogger<PreviewCouncilWorkspaceCodeDomFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Read-only structured source preview within the existing repository workspace flow.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace.codedom.preview", "POST", "/api/dxai/functions/council.artifact_workspace.codedom.preview/invoke",
        "Previews a typed C# CodeDOM source file using LocalGPT's existing reviewed generator, targeting an existing staged or generated Council workspace.",
        "workspaceName and codeDomType are required; includeSandboxEntryPoint optionally generates a standalone sample entry point. This does not write or execute source.",
        "Read-only. CodeDOM supports its existing typed declaration surface, not arbitrary modern C# refactoring.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["workspaceName","codeDomType"],"properties":{"workspaceName":{"type":"string"},"codeDomType":{"type":"object","required":["relativePath","namespace","typeName","methodName"],"properties":{"relativePath":{"type":"string"},"namespace":{"type":"string"},"typeName":{"type":"string"},"methodName":{"type":"string"},"methodResult":{"type":"string"},"summary":{"type":"string"}},"additionalProperties":false},"includeSandboxEntryPoint":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Generates preview bytes without mutating the selected workspace.</summary>
    /// <param name="request">Typed CodeDOM declaration.</param>
    /// <param name="cancellationToken">Cancels the preview.</param>
    /// <returns>Generated text and its content digest.</returns>
    public Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var binding = json.Bind<CouncilWorkspaceCodeDomRequest>(request.Parameters);
            if (!binding.Succeeded) return Task.FromResult(json.InvalidParameters(binding.Error));
            var p = binding.Value;
            if (string.IsNullOrWhiteSpace(p.WorkspaceName) || p.CodeDomType is null ||
                !string.Equals(Path.GetExtension(p.CodeDomType.RelativePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return Task.FromResult(json.InvalidParameters("Existing workspaceName and a codeDomType with .cs relativePath are required."));
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, p.WorkspaceName, logger);
            if (workspace is null) return Task.FromResult(new DxAiFunctionInvocationResult { Status = "NotFound", Error = "Workspace was not found." });
            var destination = runtime.ResolveWorkspaceTextFile(workspace, p.CodeDomType.RelativePath, allowMissing: true, logger);
            if (destination is null) return Task.FromResult(json.InvalidParameters("CodeDOM relativePath is outside the workspace or unsupported."));
            var source = p.IncludeSandboxEntryPoint
                ? codeDom.GenerateCSharpSandboxProgram(p.CodeDomType)
                : codeDom.GenerateCSharpSource(p.CodeDomType);
            return Task.FromResult(json.Success(new
            {
                p.WorkspaceName, p.CodeDomType.RelativePath, Content = source,
                SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))),
                Message = "CodeDOM source preview only; writing and guest execution require separate approval."
            }));
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Council CodeDOM preview canceled."); throw; }
        catch (Exception exception) { logger.LogWarning(exception, "Council CodeDOM preview rejected a declaration."); return Task.FromResult(json.InvalidParameters(exception.Message)); }
    }
}

/// <summary>Materializes a reviewed CodeDOM declaration into the same staged workspace used for ordinary source edits.</summary>
/// <param name="json">DXFunction JSON binding and response service.</param>
/// <param name="codeDom">Existing shared reviewed CodeDOM emitter.</param>
/// <param name="artifacts">Application-owned Council workspace root.</param>
/// <param name="runtime">Workspace path boundary.</param>
/// <param name="catalog">Database-controlled source file size policy.</param>
/// <param name="logger">Operational diagnostics.</param>
public sealed class WriteCouncilWorkspaceCodeDomFunction(
    IDxAiFunctionJsonService json,
    ICodeDomSourceService codeDom,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    ILogger<WriteCouncilWorkspaceCodeDomFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Separate approval-gated CodeDOM materialization, not code execution.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace.codedom.write", "POST", "/api/dxai/functions/council.artifact_workspace.codedom.write/invoke",
        "Generates a typed C# CodeDOM class through the existing CodeDOM service and saves it to a reviewed isolated Council source workspace.",
        "workspaceName and codeDomType required. includeSandboxEntryPoint may add a sample entry point; no build or execution occurs.",
        "Fresh human approval required. Preserves existing files until their exact destination is explicitly approved. Never writes into the originating repository.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["workspaceName","codeDomType"],"properties":{"workspaceName":{"type":"string"},"codeDomType":{"type":"object","required":["relativePath","namespace","typeName","methodName"],"properties":{"relativePath":{"type":"string"},"namespace":{"type":"string"},"typeName":{"type":"string"},"methodName":{"type":"string"},"methodResult":{"type":"string"},"summary":{"type":"string"}},"additionalProperties":false},"includeSandboxEntryPoint":{"type":"boolean"}},"additionalProperties":false}""");

    /// <summary>Writes generated source only after the exact DXFunction invocation is approved.</summary>
    /// <param name="request">Approved CodeDOM declaration and destination.</param>
    /// <param name="cancellationToken">Cancels source generation or the write.</param>
    /// <returns>Saved source path, size and content hash.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!request.UserConfirmed)
                return new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh approval is required before saving generated CodeDOM source." };
            var binding = json.Bind<CouncilWorkspaceCodeDomRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            if (string.IsNullOrWhiteSpace(p.WorkspaceName) || p.CodeDomType is null ||
                !string.Equals(Path.GetExtension(p.CodeDomType.RelativePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return json.InvalidParameters("Existing workspaceName and a codeDomType with .cs relativePath are required.");
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, p.WorkspaceName, logger);
            if (workspace is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "Workspace was not found." };
            var destination = runtime.ResolveWorkspaceTextFile(workspace, p.CodeDomType.RelativePath, allowMissing: true, logger);
            if (destination is null) return json.InvalidParameters("CodeDOM relativePath is outside the workspace or unsupported.");
            var source = p.IncludeSandboxEntryPoint
                ? codeDom.GenerateCSharpSandboxProgram(p.CodeDomType)
                : codeDom.GenerateCSharpSource(p.CodeDomType);
            var bytes = Encoding.UTF8.GetByteCount(source);
            if (catalog.MaxSingleFileBytes > 0 && bytes > catalog.MaxSingleFileBytes)
                return json.InvalidParameters("Generated source exceeds the configured MaxSingleFileBytes policy.");
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? workspace);
            await File.WriteAllTextAsync(destination, source, cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Approved CodeDOM source saved to Council workspace {WorkspaceName}, relative path {RelativePath}; source omitted from logs.", p.WorkspaceName, p.CodeDomType.RelativePath);
            return json.Success(new { p.WorkspaceName, p.CodeDomType.RelativePath, Length = bytes,
                SourceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))),
                Message = "CodeDOM source written. Review integration points, run the repository guards and refresh the ZIP separately." });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Approved Council CodeDOM write canceled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Approved Council CodeDOM write failed; source omitted from logs."); return new DxAiFunctionInvocationResult { Status = "Failed", Error = "CodeDOM source could not be saved; inspect application logs." }; }
    }
}

/// <summary>Connects a reviewed workspace file to the installed isolated runtime without mounting the repository.</summary>
/// <param name="json">DXFunction JSON binding and response service.</param>
/// <param name="sandboxes">Existing Hyperlight guest runner.</param>
/// <param name="artifacts">Application-owned Council workspace root.</param>
/// <param name="runtime">Workspace path boundary.</param>
/// <param name="catalog">Database-controlled source file size policy.</param>
/// <param name="logger">Operational diagnostics.</param>
public sealed class RunCouncilWorkspaceFileSandboxFunction(
    IDxAiFunctionJsonService json,
    ISessionSandboxExecutionService sandboxes,
    ICouncilArtifactService artifacts,
    CouncilRuntimeService runtime,
    LocalGptCatalogService catalog,
    ILogger<RunCouncilWorkspaceFileSandboxFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Approved snippet execution sourced directly from the current editable workspace.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.artifact_workspace.sandbox.run", "POST", "/api/dxai/functions/council.artifact_workspace.sandbox.run/invoke",
        "Submits one existing Council workspace source file to a configured Hyperlight guest, preserving its byte hash as a run receipt.",
        "Requires workspaceName, relativePath, advertised languageKey and expectedSourceSha256 from council.artifact_workspace_file.read. The SHA-256 must match the CURRENT source; no dependencies or full repository are mounted.",
        "Fresh human approval required. Only advertised runtime/source extension matches are allowed; no host-execution fallback.",
        IsReadOnly: false, AvailableToAi: true, RequiresHumanConfirmation: true,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: false,
        SupportsDeferredApprovalRequest: true, ApprovalRequiredBeforeCompletion: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","required":["workspaceName","relativePath","languageKey","expectedSourceSha256"],"properties":{"workspaceName":{"type":"string"},"relativePath":{"type":"string"},"languageKey":{"type":"string"},"expectedSourceSha256":{"type":"string","minLength":64,"maxLength":64},"sessionId":{"type":["string","null"],"format":"uuid"},"conversationId":{"type":["string","null"],"format":"uuid"},"projectId":{"type":["string","null"],"format":"uuid"}},"additionalProperties":false}""");

    /// <summary>Queues current workspace source bytes as one approved isolated guest run.</summary>
    /// <param name="request">Approved source path and guest language.</param>
    /// <param name="cancellationToken">Cancels submission.</param>
    /// <returns>Queued runId, source hash and status; completion is checked separately.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!request.UserConfirmed)
                return new DxAiFunctionInvocationResult { Status = "HumanApprovalRequired", Error = "Fresh approval is required before guest execution." };
            var binding = json.Bind<CouncilWorkspaceSandboxRunRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            if (string.IsNullOrWhiteSpace(p.WorkspaceName) || string.IsNullOrWhiteSpace(p.RelativePath) ||
                string.IsNullOrWhiteSpace(p.LanguageKey) || p.ExpectedSourceSha256.Length != 64 ||
                !p.ExpectedSourceSha256.All(Uri.IsHexDigit))
                return json.InvalidParameters("workspaceName, relativePath, languageKey and a 64-character inspected source SHA-256 are required.");
            var workspace = runtime.ResolveArtifactWorkspace(artifacts.ArtifactRoot, p.WorkspaceName, logger);
            if (workspace is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "Workspace was not found." };
            var path = runtime.ResolveWorkspaceTextFile(workspace, p.RelativePath, allowMissing: false, logger);
            if (path is null) return json.InvalidParameters("Source file is missing, outside the workspace or unsupported.");
            var language = sandboxes.Languages.FirstOrDefault(item => string.Equals(item.Key, p.LanguageKey, StringComparison.Ordinal));
            if (language is null || !string.Equals(Path.GetExtension(path), language.SourceExtension, StringComparison.OrdinalIgnoreCase))
                return json.InvalidParameters("The source extension does not match the selected provisioned guest language.");
            var length = new FileInfo(path).Length;
            if ((catalog.MaxSingleFileBytes > 0 && length > catalog.MaxSingleFileBytes) || length > 640_000)
                return json.InvalidParameters("Source exceeds the configured workspace or guest submission limit.");
            var source = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(source) || source.Length > 160_000)
                return json.InvalidParameters("Guest source must be non-empty and at most 160000 characters.");
            var currentSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
            if (!string.Equals(p.ExpectedSourceSha256, currentSha256, StringComparison.OrdinalIgnoreCase))
                return json.InvalidParameters("Workspace source bytes changed since inspection; read the file and approve its new hash before executing.");
            var sessionId = p.SessionId ?? p.ConversationId ?? request.ConversationId ?? Guid.NewGuid();
            var run = sandboxes.Start(new SessionSandboxRunRequest(sessionId, p.ConversationId ?? request.ConversationId,
                p.ProjectId ?? request.ProjectId, language.Key, source,
                "WorkspaceFile", UserConfirmed: true));
            logger.LogInformation("Approved workspace guest submission queued as {RunId}; source omitted from logs.", run.RunId);
            return json.Success(new { run.RunId, run.SessionId, run.Status, run.SourceSha256, p.WorkspaceName, p.RelativePath,
                Message = "Queued; poll session.sandbox.runs for a finished receipt. This is a single source file, not a repository build." });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Council workspace guest submission canceled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Council workspace guest submission failed; source omitted from logs."); return new DxAiFunctionInvocationResult { Status = "Failed", Error = "Workspace source could not be submitted to the guest; inspect application logs." }; }
    }
}

/// <summary>Turns actual generated or staged artifact evidence into a single Council completion receipt.</summary>
/// <param name="json">DXFunction JSON binding and response service.</param>
/// <param name="reviews">Reviewed code-generation state service.</param>
/// <param name="artifacts">Application-owned Council artifact root.</param>
/// <param name="stages">Repository staging provenance verifier.</param>
/// <param name="logger">Operational diagnostics.</param>
public sealed class VerifyCouncilDevelopmentArtifactFunction(
    IDxAiFunctionJsonService json,
    ICodeGenerationWorkflowService reviews,
    ICouncilArtifactService artifacts,
    ICouncilDevelopmentWorkspaceService stages,
    ILogger<VerifyCouncilDevelopmentArtifactFunction> logger) : IDxAiFunctionHandler
{
    /// <summary>Shared evidence requirement for both configured greenfield and existing-project work.</summary>
    public DxaichatFunctionInfo Descriptor { get; } = new(
        "council.development.artifact.verify", "POST", "/api/dxai/functions/council.development.artifact.verify/invoke",
        "Checks actual reviewed CodeDOM/plain source generation OR an edited staged repository with its complete refreshed ZIP. Never infers success from model prose.",
        "Provide exactly one: reviewId for approved codegen.review.execute, or workspaceName for edited council.repository.workspace.stage source with council.artifact_workspace_zip.",
        "Read-only. An unapproved review, failed build, unchanged staging copy, missing ZIP or stale archive is NOT a completed artifact. Build/test receipts remain independent.",
        IsReadOnly: true, AvailableToAi: true, RequiresHumanConfirmation: false,
        SupportsDirectInvocation: true, SupportsAutomaticInvocation: true, Source: "DIHandler",
        ParameterSchemaJson: """{"type":"object","properties":{"reviewId":{"type":["string","null"],"format":"uuid"},"workspaceName":{"type":"string"}},"additionalProperties":false}""");

    /// <summary>Returns a success receipt only after file/ZIP evidence actually exists.</summary>
    /// <param name="request">One selected source generation or workspace artifact.</param>
    /// <param name="cancellationToken">Cancels evidence checks.</param>
    /// <returns>Verified download URL or a non-success diagnostic.</returns>
    public async Task<DxAiFunctionInvocationResult> InvokeAsync(DxAiFunctionInvocationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binding = json.Bind<CouncilDevelopmentArtifactVerificationRequest>(request.Parameters);
            if (!binding.Succeeded) return json.InvalidParameters(binding.Error);
            var p = binding.Value;
            if ((p.ReviewId is null) == string.IsNullOrWhiteSpace(p.WorkspaceName))
                return json.InvalidParameters("Select exactly one reviewId or workspaceName.");
            if (p.ReviewId is Guid reviewId)
            {
                var review = await reviews.GetReviewAsync(reviewId, cancellationToken).ConfigureAwait(false);
                if (review is null) return new DxAiFunctionInvocationResult { Status = "NotFound", Error = "Selected code-generation review was not found." };
                if (review.Status != CodeGenerationReviewStatuses.Generated && review.Status != CodeGenerationReviewStatuses.BuildPassed)
                    return new DxAiFunctionInvocationResult { Status = "NotReady", Error = "Reviewed generation has not produced a successful artifact; current status: " + review.Status };
                if (string.IsNullOrWhiteSpace(review.ZipFileName) ||
                    Path.GetFileName(review.ZipFileName) != review.ZipFileName)
                    return new DxAiFunctionInvocationResult { Status = "NotReady", Error = "Reviewed generation has no safe source ZIP name." };
                var zip = Path.Combine(artifacts.ArtifactRoot, review.ZipFileName);
                if (!File.Exists(zip)) return new DxAiFunctionInvocationResult { Status = "NotReady", Error = "Reviewed generation ZIP is missing." };
                using var archive = ZipFile.OpenRead(zip);
                if (!archive.Entries.Any(entry => !entry.FullName.EndsWith("/", StringComparison.Ordinal)))
                    return new DxAiFunctionInvocationResult { Status = "NotReady", Error = "Reviewed generation ZIP contains no files." };
                return json.Success(new { Mode = "ReviewedGeneration", review.Id, review.Status, review.BuildStatus,
                    review.ReviewHash, review.WorkspaceName,
                    DownloadUrl = $"/__artifacts/council/{Uri.EscapeDataString(review.ZipFileName)}",
                    Message = "Approved generated source archive verified; build status is reported independently." });
            }
            var evidence = await stages.VerifyStageArchiveAsync(artifacts.ArtifactRoot, p.WorkspaceName.Trim(), cancellationToken).ConfigureAwait(false);
            if (!evidence.Completed)
                return new DxAiFunctionInvocationResult { Status = "NotReady", Error = evidence.Diagnostic };
            return json.Success(new { Mode = "ExistingRepository", WorkspaceName = p.WorkspaceName.Trim(),
                evidence.DownloadUrl, ChangedFileCount = evidence.ChangedPaths.Count, evidence.ChangedPaths,
                evidence.Diagnostic });
        }
        catch (OperationCanceledException exception) { logger.LogDebug(exception, "Council development artifact verification canceled."); throw; }
        catch (Exception exception) { logger.LogError(exception, "Council development artifact verification failed; content omitted from logs."); return new DxAiFunctionInvocationResult { Status = "Failed", Error = "Development artifact could not be verified; inspect application logs." }; }
    }
}
