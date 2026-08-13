using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates engineering benchmark behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    /// <param name="artifactService">Council artifact service dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="knowledgeService">Council knowledge service dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="learnBaseImporter">Learn base knowledge importer service dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="artifactBuildExecutor">Artifact build executor dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
    /// <param name="councilRuntime">Council runtime service dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    /// <param name="councilText">Council text service dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
    public sealed class EngineeringBenchmarkService(
        ICouncilArtifactService artifactService,
        ICouncilKnowledgeService knowledgeService,
        ILearnBaseKnowledgeImporterService learnBaseImporter,
        IArtifactBuildExecutor artifactBuildExecutor,
        ILogger<EngineeringBenchmarkService> logger,
        CouncilRuntimeService councilRuntime,
        CouncilTextService councilText) : IEngineeringBenchmarkService
    {
        /// <summary>
        /// Performs run as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The engineering benchmark result produced by the operation.</returns>
        public async Task<EngineeringBenchmarkResult> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new EngineeringBenchmarkResult
                {
                    TaskSet = councilText.NormalizeTaskSet(request.TaskSet, logger)
                };
                if (request.ImportLearnBaseFirst)
                    result.LearnBaseImport = await learnBaseImporter.ImportAsync(new LearnBaseImportRequest
                    {
                        RootPath = request.LearnBaseRootPath,
                        SaveToKnowledge = true,
                        MaxProjects = 40
                    }, cancellationToken).ConfigureAwait(false);

                foreach (var task in councilRuntime.BuildTasks(result.TaskSet, logger))
                {
                    var taskResult = new EngineeringBenchmarkTaskResult
                    {
                        TaskId = task.Id,
                        Name = task.Name,
                        Prompt = task.Prompt
                    };

                    taskResult.Lanes.Add(councilRuntime.NotRunLane("A. raw Ollama model", "Live raw Ollama call intentionally not run in this deterministic benchmark. Run later with GPU-safe caps and record the transcript.",logger));
                    if (request.RunLocalGptArtifacts && request.UserConfirmedArtifactActions)
                    {
                        var runLocalGptLaneAsync = await RunLocalGptLaneAsync(task, request, cancellationToken, logger).ConfigureAwait(false);
                        ArgumentNullException.ThrowIfNull(runLocalGptLaneAsync);
                        taskResult.Lanes.Add(runLocalGptLaneAsync);
                    }
                    else if (request.RunLocalGptArtifacts)
                    {
                        taskResult.Lanes.Add(councilRuntime.NotRunLane(
                            "B. LocalGPT with DxFunctions + memory",
                            "Skipped because fresh human confirmation for artifact generation/build validation was not supplied.",
                            logger));
                        result.Warnings.Add("Artifact benchmark actions were skipped because fresh human confirmation was not supplied.");
                    }
                    else
                    {
                        taskResult.Lanes.Add(councilRuntime.NotRunLane("B. LocalGPT with DxFunctions + memory", "Skipped by request.", logger));
                    }

                    taskResult.Lanes.Add(councilRuntime.NotRunLane("C. cloud coding assistant", "Cloud comparison must be run with a real prompt/session and pasted evidence; not faked by LocalGPT.",logger));
                    taskResult.Lanes.Add(councilRuntime.BuildManualExpectedLane(task,logger));
                    result.Tasks.Add(taskResult);
                }

                result.CompletedAtUtc = DateTime.UtcNow;
                if (request.SaveToKnowledge)
                    result.KnowledgeEntryId = await SaveBenchmarkKnowledgeAsync(result, cancellationToken).ConfigureAwait(false);

                logger.LogInformation("Engineering benchmark {RunId} completed with {TaskCount} task(s).", result.RunId, result.Tasks.Count);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Engineering benchmark failed.");
                return new EngineeringBenchmarkResult
                {
                    TaskSet = request?.TaskSet ?? "engineering",
                    CompletedAtUtc = DateTime.UtcNow,
                    Warnings = [$"{ex.GetType().Name}: {ex.Message}"]
                };
            }
        }
        /// <summary>
        /// Persists benchmark knowledge as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the engineering benchmark operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The GUID produced by the operation.</returns>
        public async Task<Guid?> SaveBenchmarkKnowledgeAsync(EngineeringBenchmarkResult result, CancellationToken cancellationToken)
        {
            try
            {
                var summary = new StringBuilder()
               .AppendLine($"Engineering benchmark run {result.RunId} completed at {result.CompletedAtUtc:O}.")
               .AppendLine($"Task set: {result.TaskSet}.")
               .AppendLine($"Tasks: {string.Join("; ", result.Tasks.Select(task => task.Name))}.")
               .AppendLine("Lane rule: raw Ollama and cloud lanes must be run with real transcripts before scoring; deterministic LocalGPT artifacts are allowed for no-GPU smoke evidence.");

                foreach (var task in result.Tasks)
                {
                    var local = task.Lanes.FirstOrDefault(lane => lane.Lane.StartsWith("B.", StringComparison.Ordinal));
                    summary.AppendLine($"- {task.Name}: LocalGPT status {local?.Status}, score {local?.TotalScore}, artifacts {local?.Artifacts.Count ?? 0}.");
                    if (local?.MissingFiles.Count > 0)
                        summary.AppendLine($"  Missing: {string.Join(", ", local.MissingFiles)}");
                    if (local?.BuildChecks.Count > 0)
                        summary.AppendLine($"  Build checks: {string.Join(", ", local.BuildChecks.Select(check => $"{check.ArtifactName}={check.Status}"))}");
                }

                var entry = await knowledgeService.SaveEntryAsync(new CouncilKnowledgeEntry
                {
                    Topic = "Personal engineering benchmark: LocalGPT vs raw models",
                    Scope = "Benchmark",
                    Source = $"/__diag/benchmark/engineering {result.RunId}",
                    Content = summary.ToString(),
                    HelpfulSources = "Use selected learn-base knowledge entries, artifact zips, build checks, raw Ollama transcripts, and cloud assistant transcripts. Do not fake unrun lanes.",
                    Tags = "benchmark; localgpt; ollama; devexpress; blazor; minecraft; artifacts",
                    Confidence = 70,
                    VerificationStatus = "SourceBacked",
                    IsPinned = true
                }, cancellationToken).ConfigureAwait(false);

                return entry.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ValidateBuildableArtifactAsync");
                return null;
            }
        }
        /// <summary>
        /// Performs run LocalGPT lane as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="task">Task value supplied to the engineering benchmark operation and used when producing its result.</param>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The engineering benchmark lane result produced by the operation.</returns>
        private async Task<EngineeringBenchmarkLaneResult?> RunLocalGptLaneAsync(
            BenchmarkTaskDefinition task,
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var councilResult = new MultiModelCouncilResult
                {
                    Prompt = task.Prompt,
                    ModelNames = ["LocalGPT deterministic artifact service"],
                    FinalAnswer = task.LocalGptFinalAnswer
                };
                var councilRequest = new MultiModelCouncilRequest
                {
                    Prompt = task.Prompt,
                    ModelNames = ["artifact-benchmark"],
                    GenerateImplementationArtifact = true,
                    UserConfirmedArtifactBuild = request.UserConfirmedArtifactActions,
                    MaxOutputTokens = 1024,
                    MaxContextTokens = 2048,
                    MaxRounds = 0
                };

                var artifacts = await artifactService.CreateImplementationArtifactsAsync(councilRequest, councilResult, cancellationToken).ConfigureAwait(false);
                stopwatch.Stop();

                var lane = new EngineeringBenchmarkLaneResult
                {
                    Lane = "B. LocalGPT with DxFunctions + memory",
                    Status = artifacts.Count > 0 ? "Ran" : "RanNoArtifact",
                    Duration = stopwatch.Elapsed,
                    Artifacts = artifacts.ToList(),
                    TimeToUsableOutputScore = stopwatch.Elapsed < TimeSpan.FromSeconds(20) ? 10 : stopwatch.Elapsed < TimeSpan.FromMinutes(1) ? 8 : 5,
                    RepairPromptsScore = 10,
                    RepairPromptCount = 0,
                    DownloadableArtifactScore = artifacts.Any(artifact => !string.IsNullOrWhiteSpace(artifact.DownloadUrl)) ? 10 : 0
                };

                var artifactEntries = artifacts
                    .Where(artifact => artifact.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(filter => councilRuntime.ReadZipEntriesSafe(filter,logger))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                lane.Evidence.AddRange(artifacts.Select(artifact => $"{artifact.Kind}: {artifact.Name} -> {artifact.DownloadUrl}"));
                lane.MissingFiles.AddRange(task.RequiredArtifactEntries.Where(required => !councilText.ContainsZipEntry(artifactEntries, required, logger)));
                lane.MissingFilesScore = lane.MissingFiles.Count == 0 ? 10 : Math.Max(0, 10 - lane.MissingFiles.Count * 2);
                lane.ValidArchitectureScore = councilRuntime.ScoreArchitecture(task, artifactEntries, artifacts, logger);
                if (request.ValidateBuildableArtifacts)
                    lane.BuildChecks.AddRange(await ValidateBuildableArtifactsAsync(
                        artifacts,
                        request.MaxBuildArtifacts,
                        request.UserConfirmedArtifactActions,
                        cancellationToken).ConfigureAwait(false));

                lane.BuildabilityScore = councilRuntime.ScoreBuildability(task, artifacts, lane.BuildChecks, request.ValidateBuildableArtifacts, logger);
                lane.WrongPackagesTemplatesScore = councilRuntime.ScoreWrongTemplateRisk(task, artifactEntries, logger);
                lane.TotalScore = councilRuntime.SumScores(lane, logger);
                lane.Notes = lane.MissingFiles.Count == 0
                    ? "Deterministic LocalGPT artifact path produced expected benchmark files."
                    : "Artifact was produced, but required benchmark entries were missing. This is improvement fuel, not a pass.";
                if (request.ValidateBuildableArtifacts && lane.BuildChecks.Count > 0)
                    lane.Notes += $" Build checks: {string.Join("; ", lane.BuildChecks.Select(check => $"{check.ArtifactName}={check.Status}"))}.";
                return lane;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "RunLocalGptLaneAsync");
                return null;
            }
         
        }

        /// <summary>
        /// Validates buildable artifacts as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="artifacts">Council artifact dependency used by the engineering benchmark workflow to provide the corresponding application capability.</param>
        /// <param name="maxBuildArtifacts">Max build artifacts value supplied to the engineering benchmark operation and used when producing its result.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The collection produced by the operation.</returns>
        private async Task<IReadOnlyList<EngineeringBenchmarkBuildCheck>> ValidateBuildableArtifactsAsync(
            IReadOnlyList<CouncilArtifact> artifacts,
            int maxBuildArtifacts,
            bool userConfirmed,
            CancellationToken cancellationToken)
        {
    try
    {
                var checks = new List<EngineeringBenchmarkBuildCheck>();
                foreach (var artifact in artifacts
                    .Where(item => item.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(item.FilePath))
                    .Take(Math.Clamp(maxBuildArtifacts, 1, 8)))
                {
                    var check = await ValidateBuildableArtifactAsync(artifact, userConfirmed, cancellationToken).ConfigureAwait(false);
                    checks.Add(check);
                }
                return checks;
        
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(EngineeringBenchmarkService)}.{nameof(ValidateBuildableArtifactsAsync)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(EngineeringBenchmarkService)}.{nameof(ValidateBuildableArtifactsAsync)} failed.");
        throw;
    }
}

        /// <summary>
        /// Validates buildable artifact as part of the engineering benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="artifact">Artifact value supplied to the engineering benchmark operation and used when producing its result.</param>
        /// <param name="userConfirmed">Value indicating whether user confirmed should apply to this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The engineering benchmark build check produced by the operation.</returns>
        private async Task<EngineeringBenchmarkBuildCheck> ValidateBuildableArtifactAsync(
            CouncilArtifact artifact,
            bool userConfirmed,
            CancellationToken cancellationToken)
        {
            var startedAt = DateTime.UtcNow;
            var root = Path.Combine(
                Path.GetTempPath(),
                "LocalGPT",
                "EngineeringBenchmarkBuilds",
                $"{Path.GetFileNameWithoutExtension(artifact.Name)}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);

            var check = new EngineeringBenchmarkBuildCheck
            {
                ArtifactName = artifact.Name,
                ExtractedRoot = root
            };

            try
            {
                ZipFile.ExtractToDirectory(artifact.FilePath, root, overwriteFiles: true);
                var solutionPath = Directory
                    .EnumerateFiles(root, "*.sln", SearchOption.AllDirectories)
                    .OrderBy(path => path.Length)
                    .FirstOrDefault();

                if (solutionPath is null)
                {
                    check.Status = "NoSolution";
                    check.OutputPreview = "No .sln file found. This artifact is not a .NET build target.";
                    return check;
                }

                check.SolutionPath = solutionPath;
                var build = await artifactBuildExecutor.BuildAsync(
                    solutionPath,
                    root,
                    "Debug",
                    null,
                    TimeSpan.FromMinutes(3),
                    cancellationToken,
                    userConfirmed: userConfirmed).ConfigureAwait(false);

                check.ExitCode = build.ExitCode ?? 0;
                check.Status = build.Status;
                check.OutputPreview = councilText.TrimForPrompt(build.StandardOutput, 1800, logger);
                check.ErrorPreview = councilText.TrimForPrompt(build.StandardError, 1200, logger);
                return check;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                check.Status = "BuildCheckError";
                check.ErrorPreview = councilText.TrimForPrompt(ex.Message, 1200, logger);
                logger.LogWarning(ex, "Artifact build validation failed for {ArtifactName}.", artifact.Name);
                return check;
            }
            finally
            {
                check.Duration = DateTime.UtcNow - startedAt;
            }
        }
    }
}
