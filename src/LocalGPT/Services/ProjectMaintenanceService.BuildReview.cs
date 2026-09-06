using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalGPT.BusinessObjects;
using LocalGPT.BusinessObjects.EFCore;
using LocalGPT.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates project maintenance behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class ProjectMaintenanceService
    {
    /// <summary>
    /// Performs run build verification as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    public async Task<ProjectBuildVerification> RunBuildVerificationAsync(Guid projectId, RunProjectBuildVerificationRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "executing the selected compiler against the project revision");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            var revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == request.RevisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The project revision was not found.");
            var workspaceCandidates = await db.ProjectWorkspaceRoots.Where(item => item.IsEnabled && (item.ProjectId == null || item.ProjectId == projectId)).OrderBy(item => item.Priority).ToListAsync(cancellationToken).ConfigureAwait(false);
            var workspace = workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Project" && item.ProjectId == projectId)
                ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "ProjectType" && RegexMatches(item.ProjectTypePattern, project.ProjectType))
                ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Global" && item.IsDefault)
                ?? workspaceCandidates.FirstOrDefault(item => item.ScopeKind == "Global");
            if (workspace is not null)
            {
                if (workspace.LastPermissionCheckedAtUtc is null)
                    throw new InvalidOperationException("Assess the selected workspace permissions before running a compiler in it.");
                if (string.Equals(workspace.LastPermissionStatus, "Danger", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The selected workspace permission assessment contains danger findings. Correct them before build execution.");
                if (!workspace.LastPermissionReadAccess || !workspace.LastPermissionWriteAccess)
                    throw new InvalidOperationException("The selected workspace has not proven the read and write access required for compiler execution. Run the rights assessment with the bounded write probe first.");
            }

            var compilerId = request.CompilerInstallationId != Guid.Empty ? request.CompilerInstallationId : workspace?.PreferredCompilerInstallationId ?? Guid.Empty;
            var compiler = await db.ProjectCompilerInstallations.SingleOrDefaultAsync(item => item.Id == compilerId && item.IsEnabled, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The selected or workspace-assigned compiler installation was not found or is disabled.");
            if (!compiler.LastValidationSucceeded)
                throw new InvalidOperationException("Validate the selected compiler installation successfully before using it for a revision build.");

            var root = NormalizeAbsolutePath(!string.IsNullOrWhiteSpace(revision.SourceRootPath) ? revision.SourceRootPath : project.RootPath, nameof(project.RootPath));
            var configuredSolution = !string.IsNullOrWhiteSpace(revision.SolutionPath) ? revision.SolutionPath : project.SolutionPath;
            var target = File.Exists(configuredSolution) && IsPathInside(root, configuredSolution) ? configuredSolution : root;
            var trackedFiles = await db.LocalGptProjectTrackedFiles.AsNoTracking()
                .Where(item => item.ProjectId == projectId && item.RevisionId == revision.Id && item.Exists && item.IsUserApproved && !item.IsGenerated)
                .OrderBy(item => item.ProjectRelativePath)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (trackedFiles.Count == 0)
                throw new InvalidOperationException("Scan the selected revision before running its build verification.");
            var beforeState = await CaptureTrackedSourceStateAsync(trackedFiles, requireStoredHashMatch: true, cancellationToken).ConfigureAwait(false);

            var arguments = !string.IsNullOrWhiteSpace(request.Arguments)
                ? request.Arguments.Trim()
                : !string.IsNullOrWhiteSpace(workspace?.BuildArguments)
                    ? workspace.BuildArguments.Trim()
                    : DefaultBuildArguments(compiler.Language, target, request.Configuration);
            var executionEnvironmentJson = MergeEnvironmentJson(compiler.EnvironmentVariablesJson, workspace?.EnvironmentVariablesJson);
            var timeout = Math.Clamp(request.TimeoutSeconds, 10, 7200);
            var outputDirectory = LocalGptApplicationDataPaths.ResolveUserPath("BuildVerifications", projectId.ToString("N"));
            Directory.CreateDirectory(outputDirectory);
            var verification = new ProjectBuildVerification
            {
                ProjectId = projectId,
                RevisionId = revision.Id,
                CompilerInstallationId = compiler.Id,
                Configuration = TrimOrFallback(request.Configuration, 80, "Debug"),
                ExecutablePath = compiler.ExecutablePath,
                Arguments = arguments,
                WorkingDirectory = root,
                StartedAtUtc = DateTime.UtcNow,
                SourceSnapshotHash = beforeState.Hash
            };
            verification.OutputLogPath = Path.Combine(outputDirectory, verification.Id.ToString("N") + ".log");
            verification.EvidenceManifestPath = Path.Combine(outputDirectory, verification.Id.ToString("N") + ".manifest.json");
            db.ProjectBuildVerifications.Add(verification);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var build = await RunProcessAsync(compiler.ExecutablePath, arguments, root, executionEnvironmentJson, timeout, cancellationToken).ConfigureAwait(false);
            var testsExecuted = build.ExitCode == 0 && !string.IsNullOrWhiteSpace(request.TestArguments);
            var testsExitCode = 0;
            var combined = new StringBuilder().AppendLine("BUILD").AppendLine(build.Output);
            if (testsExecuted)
            {
                var tests = await RunProcessAsync(compiler.ExecutablePath, request.TestArguments.Trim(), root, executionEnvironmentJson, timeout, cancellationToken).ConfigureAwait(false);
                testsExitCode = tests.ExitCode;
                combined.AppendLine().AppendLine("TESTS").AppendLine(tests.Output);
            }
            var afterState = await CaptureTrackedSourceStateAsync(trackedFiles, requireStoredHashMatch: false, cancellationToken).ConfigureAwait(false);
            var sourceChanged = !string.Equals(beforeState.Hash, afterState.Hash, StringComparison.Ordinal);
            var output = Limit(
                combined.ToString(),
                Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ProjectMaintenanceMaximumCapturedCharacters)));
            await File.WriteAllTextAsync(verification.OutputLogPath, output, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            var evidence = JsonSerializer.Serialize(new
            {
                verification.Id,
                ProjectId = projectId,
                RevisionId = revision.Id,
                CompilerId = compiler.Id,
                Compiler = new { compiler.Name, compiler.Language, compiler.Version, compiler.Architecture, compiler.ExecutablePath, compiler.CompilerHomePath },
                WorkingDirectory = root,
                Workspace = workspace is null ? null : new { workspace.Id, workspace.Name, workspace.EnvironmentKind, workspace.EnvironmentRootPath, workspace.LastPermissionStatus },
                Target = target,
                BuildArguments = arguments,
                TestArguments = testsExecuted ? request.TestArguments.Trim() : string.Empty,
                BuildExitCode = build.ExitCode,
                TestsExecuted = testsExecuted,
                TestsExitCode = testsExecuted ? testsExitCode : (int?)null,
                SourceHashBefore = beforeState.Hash,
                SourceHashAfter = afterState.Hash,
                SourceChangedDuringVerification = sourceChanged,
                Files = beforeState.Entries
            }, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(verification.EvidenceManifestPath, evidence, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);

            verification.CompletedAtUtc = DateTime.UtcNow;
            verification.ExitCode = build.ExitCode;
            verification.SourceChangedDuringVerification = sourceChanged;
            verification.BuildSucceeded = build.ExitCode == 0 && !sourceChanged;
            verification.TestsExecuted = testsExecuted;
            verification.TestsSucceeded = testsExecuted && testsExitCode == 0 && !sourceChanged;
            verification.OutputHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output)));
            verification.Summary = sourceChanged
                ? "Source files changed during build or test execution; rescan and repeat verification."
                : verification.BuildSucceeded && (!testsExecuted || verification.TestsSucceeded)
                    ? (testsExecuted ? "Build and requested tests succeeded for the unchanged source state." : "Build succeeded for the unchanged source state; no tests were requested.")
                    : "Build or requested tests failed; review the local evidence and log.";
            revision.CompileVerified = verification.BuildSucceeded && (!testsExecuted || verification.TestsSucceeded);
            revision.CouncilVerified = false;
            revision.ReadyForTesting = false;
            revision.SourceSnapshotHash = beforeState.Hash;
            revision.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Build verification {VerificationId} for project {ProjectId} completed: build={BuildSucceeded}, testsExecuted={TestsExecuted}, tests={TestsSucceeded}, sourceChanged={SourceChanged}, exit={ExitCode}.", verification.Id, projectId, verification.BuildSucceeded, verification.TestsExecuted, verification.TestsSucceeded, verification.SourceChangedDuringVerification, verification.ExitCode);
            return verification;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RunBuildVerificationAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RunBuildVerificationAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs record council build review as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="verificationId">Identifier of the verification to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    public async Task<ProjectBuildVerification> RecordCouncilBuildReviewAsync(Guid verificationId, RecordCouncilBuildReviewRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "recording the council build review");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var verification = await db.ProjectBuildVerifications.Include(item => item.Revision).SingleOrDefaultAsync(item => item.Id == verificationId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Build verification {verificationId} was not found.");
            verification.CouncilReviewSucceeded = request.CompileErrorsAbsent && verification.BuildSucceeded && !verification.SourceChangedDuringVerification && (!verification.TestsExecuted || verification.TestsSucceeded);
            verification.CouncilReviewSummary = Trim(request.Summary, 16000);
            if (verification.Revision is not null)
            {
                verification.Revision.CouncilVerified = verification.CouncilReviewSucceeded;
                verification.Revision.UpdatedAtUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Council review recorded for build verification {VerificationId}: success={Succeeded}.", verificationId, verification.CouncilReviewSucceeded);
            return verification;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RecordCouncilBuildReviewAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(RecordCouncilBuildReviewAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Approves revision ready for test as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="projectId">Identifier of the project to use for this operation.</param>
    /// <param name="revisionId">Identifier of the revision to use for this operation.</param>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The project build verification produced by the operation.</returns>
    public async Task<ProjectBuildVerification> ApproveRevisionReadyForTestAsync(Guid projectId, Guid revisionId, ApproveRevisionReadyForTestRequest request, CancellationToken cancellationToken = default)
    {
    try
    {
            RequireConfirmation(request.UserConfirmed, "approving a revision as ready for testing");
            await databaseInitializer.InitializeAsync(cancellationToken).ConfigureAwait(false);
            var db = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
            await using var configuredDbAsyncDisposal = db.ConfigureAwait(false);
            var project = await db.LocalGptProjects.SingleOrDefaultAsync(item => item.Id == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");
            var revision = await db.LocalGptProjectRevisions.SingleOrDefaultAsync(item => item.Id == revisionId && item.ProjectId == projectId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The project revision was not found.");
            var verification = await db.ProjectBuildVerifications.SingleOrDefaultAsync(item => item.Id == request.VerificationId && item.RevisionId == revisionId, cancellationToken).ConfigureAwait(false)
                ?? throw new KeyNotFoundException("The selected verification does not belong to the revision.");
            if (!verification.BuildSucceeded || verification.SourceChangedDuringVerification) throw new InvalidOperationException("The revision cannot be approved before a successful build of an unchanged source state.");
            if (request.RequireTests && (!verification.TestsExecuted || !verification.TestsSucceeded)) throw new InvalidOperationException("The revision cannot be approved before the requested tests were executed successfully.");
            if (!verification.CouncilReviewSucceeded) throw new InvalidOperationException("The revision cannot be approved before the council records a compile-error-free review.");

            var files = await db.LocalGptProjectTrackedFiles.AsNoTracking()
                .Where(item => item.ProjectId == projectId && item.RevisionId == revisionId && item.Exists && item.IsUserApproved && !item.IsGenerated)
                .OrderBy(item => item.ProjectRelativePath)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            if (files.Count == 0) throw new InvalidOperationException("Scan and approve the project files before approving a ready-for-test revision.");
            var currentState = await CaptureTrackedSourceStateAsync(files, requireStoredHashMatch: true, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(currentState.Hash, verification.SourceSnapshotHash, StringComparison.Ordinal))
                throw new InvalidOperationException("The project files changed after the successful build verification. Rescan and repeat build, tests, and council review.");

            if (request.CreateLosslessSnapshot)
            {
                var workspace = await ResolveWorkspaceAsync(projectId, cancellationToken).ConfigureAwait(false);
                Directory.CreateDirectory(workspace.RootPath);
                var directory = Path.Combine(workspace.RootPath, "LocalGPT-Revisions", SafeFileName(project.Name), revision.Id.ToString("N"));
                Directory.CreateDirectory(directory);
                var archivePath = Path.Combine(directory, "source-snapshot.zip");
                if (File.Exists(archivePath)) File.Delete(archivePath);
                using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!File.Exists(file.AbsolutePath)) throw new FileNotFoundException("A tracked source file disappeared before snapshot creation.", file.AbsolutePath);
                        archive.CreateEntryFromFile(file.AbsolutePath, file.ProjectRelativePath.Replace('\\', '/'), CompressionLevel.Optimal);
                    }
                    var entry = archive.CreateEntry(".localgpt-manifest.json", CompressionLevel.Optimal);
                    var stream = entry.Open();
                    await using var configuredStreamAsyncDisposal = stream.ConfigureAwait(false);
                    var writer = new StreamWriter(stream, new UTF8Encoding(false));
                    await using var configuredWriterAsyncDisposal = writer.ConfigureAwait(false);
                    await writer.WriteAsync(currentState.ManifestJson.AsMemory(), cancellationToken).ConfigureAwait(false);
                }
                verification.SnapshotArchivePath = archivePath;
                revision.SnapshotArchivePath = archivePath;
            }
            verification.SourceSnapshotHash = currentState.Hash;
            verification.UserApprovedReadyForTest = true;
            revision.SourceSnapshotHash = currentState.Hash;
            revision.CompileVerified = true;
            revision.CouncilVerified = true;
            revision.ReadyForTesting = true;
            revision.ApprovedForTestingAtUtc = DateTime.UtcNow;
            revision.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Revision {RevisionId} for project {ProjectId} was approved as ready for testing using verification {VerificationId} and source hash prefix {SourceHashPrefix}.", revisionId, projectId, verification.Id, currentState.Hash[..Math.Min(12, currentState.Hash.Length)]);
            return verification;
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ApproveRevisionReadyForTestAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ApproveRevisionReadyForTestAsync)} failed.");
        throw;
    }
}

    /// <summary>
    /// Normalizes compiler search roots as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<string> NormalizeCompilerSearchRoots(DiscoverProjectCompilersRequest request)
    {
    try
    {
            var textRoots = (request.CustomSearchRootsText ?? string.Empty)
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return (request.CustomSearchRoots ?? [])
                .Concat(textRoots)
                .Where(root => !string.IsNullOrWhiteSpace(root))
                .Select(root => root.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
    
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeCompilerSearchRoots)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(NormalizeCompilerSearchRoots)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs enumerate files safe as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="root">Root value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="warnings">String dependency used by the project maintenance workflow to provide the corresponding application capability.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IEnumerable<string> EnumerateFilesSafe(string root, ICollection<string> warnings)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            IEnumerable<string> files;
            try { files = Directory.EnumerateFiles(current); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add($"Could not read {current}: {ex.Message}"); continue; }
            foreach (var file in files) yield return file;
            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(current); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { warnings.Add($"Could not enumerate {current}: {ex.Message}"); continue; }
            foreach (var dir in dirs) pending.Push(dir);
        }
    }

    /// <summary>
    /// Performs run process as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="executable">Executable value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="arguments">Arguments value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="workingDirectory">Working directory value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="environmentVariablesJson">Environment variables json value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="timeoutSeconds">Timeout seconds value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
    /// <returns>The int exit code string output produced by the operation.</returns>
    private async Task<(int ExitCode, string Output)> RunProcessAsync(string executable, string arguments, string? workingDirectory, string? environmentVariablesJson, int timeoutSeconds, CancellationToken cancellationToken)
    {
        if (!File.Exists(executable)) throw new FileNotFoundException("The configured compiler executable does not exist.", executable);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory! : Path.GetDirectoryName(executable) ?? Environment.CurrentDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        if (!string.IsNullOrWhiteSpace(environmentVariablesJson))
        {
            try
            {
                var environment = JsonSerializer.Deserialize<Dictionary<string, string>>(environmentVariablesJson) ?? [];
                foreach (var pair in environment)
                    process.StartInfo.Environment[pair.Key] = pair.Value;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("The compiler environment JSON is invalid.", ex);
            }
        }
        if (!process.Start()) throw new InvalidOperationException("The compiler process could not be started.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(timeout.Token);
        try { await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        var output = Limit(
            (await stdoutTask.ConfigureAwait(false)) + Environment.NewLine + (await stderrTask.ConfigureAwait(false)),
            Math.Max(1, runtimePolicy.GetInt(LocalGptRuntimeValue.ProjectMaintenanceMaximumCapturedCharacters)));
        return (process.ExitCode, output);
    }

    /// <summary>
    /// Performs default build arguments as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="language">Language value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="target">Target value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="configuration">Configuration containing the caller-supplied values that control this operation.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DefaultBuildArguments(string language, string target, string configuration) {
    try
    {
        return language.ToLowerInvariant() switch
    {
        "dotnet" => $"build \"{target}\" --configuration \"{configuration}\" --nologo",
        "java" => $"\"{target}\"",
        "python" => $"-m compileall \"{target}\"",
        "powershell" => $"-NoProfile -NonInteractive -Command \"Get-ChildItem -LiteralPath '{target.Replace("'", "''")}' -Filter *.ps1 -Recurse | ForEach-Object {{ [void][scriptblock]::Create((Get-Content -Raw -LiteralPath $_.FullName)) }}\"",
        _ => throw new InvalidOperationException("No safe default build arguments exist for this compiler. Enter explicit reviewed arguments.")
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(DefaultBuildArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(DefaultBuildArguments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs default validation arguments as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="language">Language value supplied to the project maintenance operation and used when producing its result.</param>
    /// <param name="executable">Executable value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string DefaultValidationArguments(string language, string executable) {
    try
    {
        return language.ToLowerInvariant() switch
    {
        "powershell" when Path.GetFileName(executable).StartsWith("powershell", StringComparison.OrdinalIgnoreCase) => "-NoProfile -NonInteractive -Command \"$PSVersionTable.PSVersion.ToString()\"",
        "powershell" => "-NoProfile -NonInteractive -Command \"$PSVersionTable.PSVersion.ToString()\"",
        "java" => "-version",
        "embedded" when Path.GetFileName(executable).StartsWith("arduino-cli", StringComparison.OrdinalIgnoreCase) => "version",
        "embedded" => "--version",
        "cpp" when Path.GetFileName(executable).StartsWith("cl", StringComparison.OrdinalIgnoreCase) => "",
        _ => "--version"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(DefaultValidationArguments)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(DefaultValidationArguments)} failed.");
        throw;
    }
}

    /// <summary>
    /// Performs default patterns for as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extension">Extension value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string role string structure string content produced by the operation.</returns>
    private (string Role, string Structure, string Content) DefaultPatternsFor(string extension) => extension.ToLowerInvariant() switch
    {
        ".cs" => ("CSharpSource", @"(?m)^\s*(?:public|internal|private|protected)?\s*(?:sealed\s+|abstract\s+|static\s+|partial\s+)*(?:class|record|interface|enum|struct)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".razor" => ("RazorComponent", @"(?m)^\s*@(?:page|code|functions|inject|using)\b|<(?<component>[A-Z][A-Za-z0-9.]*)\b", @"(?s).*"),
        ".csproj" or ".props" or ".targets" => ("MSBuild", @"<(?<element>Project|PropertyGroup|ItemGroup|Target|PackageReference|ProjectReference)\b", @"(?s)^\s*<Project\b.*</Project>\s*$"),
        ".sln" or ".slnx" => ("Solution", @"(?m)^(?:Project\(|\s*<Project\b)", @"(?s).*"),
        ".json" => ("Json", "\"(?<property>[^\"]+)\"\\s*:", @"(?s)^\s*[\[{].*[\]}]\s*$"),
        ".ps1" => ("PowerShell", @"(?mi)^\s*(?:function|class|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_-]*)", @"(?s).*"),
        ".java" => ("JavaSource", @"(?m)^\s*(?:public|protected|private)?\s*(?:abstract\s+|final\s+)?(?:class|interface|enum|record)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".py" => ("PythonSource", @"(?m)^\s*(?:async\s+)?(?:def|class)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".ino" or ".pde" => ("ArduinoSketch", @"(?m)^\s*(?:void\s+(?<entry>setup|loop)\s*\(|#define\s+(?<define>[A-Za-z_][A-Za-z0-9_]*)|(?:class|struct|enum)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*))", @"(?s).*"),
        ".cpp" or ".cc" or ".cxx" or ".c" or ".h" or ".hpp" => ("CppSource", @"(?m)^\s*(?:class|struct|enum|namespace)\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)", @"(?s).*"),
        ".ini" or ".toml" or ".cfg" or ".conf" => ("ToolchainConfiguration", @"(?m)^\s*(?:\[(?<section>[^]]+)\]|(?<key>[A-Za-z_][A-Za-z0-9_.-]*)\s*=)", @"(?s).*"),
        ".cmake" or ".kconfig" or ".sdkconfig" => ("EmbeddedBuildConfiguration", @"(?mi)^\s*(?<directive>project|set|option|config|menuconfig|source|include)\b", @"(?s).*"),
        _ => ("Document", string.Empty, @"(?s).*")
    };

    /// <summary>
    /// Performs content type for as part of the project maintenance service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="extension">Extension value supplied to the project maintenance operation and used when producing its result.</param>
    /// <returns>The string produced by the operation.</returns>
    private string ContentTypeFor(string extension) {
    try
    {
        return extension.ToLowerInvariant() switch
    {
        ".json" => "application/json", ".xml" or ".csproj" or ".props" or ".targets" or ".slnx" => "application/xml",
        ".md" => "text/markdown", ".yml" or ".yaml" => "application/yaml", _ => IsTextExtension(extension) ? "text/plain" : "application/octet-stream"
    };
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ContentTypeFor)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProjectMaintenanceService)}.{nameof(ContentTypeFor)} failed.");
        throw;
    }
}

    }
}
