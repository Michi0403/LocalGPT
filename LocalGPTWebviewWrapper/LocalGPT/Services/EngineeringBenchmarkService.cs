using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using SQLitePCL;
using System.Diagnostics;
using System.IO.Compression;
using System.ServiceModel.Channels;
using System.Text;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using static System.Net.WebRequestMethods;

namespace LocalGPT.Services
{
    public sealed class EngineeringBenchmarkService(
        ICouncilArtifactService artifactService,
        ICouncilKnowledgeService knowledgeService,
        ILearnBaseKnowledgeImporterService learnBaseImporter,
        ILogger<EngineeringBenchmarkService> logger) : IEngineeringBenchmarkService
    {
        public async Task<EngineeringBenchmarkResult?> RunAsync(
            EngineeringBenchmarkRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = new EngineeringBenchmarkResult
                {
                    TaskSet = CouncilChatStringFunctions.NormalizeTaskSet(request.TaskSet, logger)
                };
                if (request.ImportLearnBaseFirst)
                    result.LearnBaseImport = await learnBaseImporter.ImportAsync(new LearnBaseImportRequest
                    {
                        RootPath = request.LearnBaseRootPath,
                        SaveToKnowledge = true,
                        MaxProjects = 40
                    }, cancellationToken);

                foreach (var task in CouncilChatStaticsGeneral.BuildTasks(result.TaskSet, logger))
                {
                    var taskResult = new EngineeringBenchmarkTaskResult
                    {
                        TaskId = task.Id,
                        Name = task.Name,
                        Prompt = task.Prompt
                    };

                    taskResult.Lanes.Add(CouncilChatStaticsGeneral.NotRunLane("A. raw Ollama model", "Live raw Ollama call intentionally not run in this deterministic benchmark. Run later with GPU-safe caps and record the transcript.",logger));
                    if (request.RunLocalGptArtifacts)
                    {
                        var runLocalGptLaneAsync = await RunLocalGptLaneAsync(task, request, cancellationToken, logger);
                        ArgumentNullException.ThrowIfNull(runLocalGptLaneAsync);
                        taskResult.Lanes.Add(runLocalGptLaneAsync);
                    }
                    else
                        taskResult.Lanes.Add(CouncilChatStaticsGeneral.NotRunLane("B. LocalGPT with DxFunctions + memory", "Skipped by request.",logger));

                    taskResult.Lanes.Add(CouncilChatStaticsGeneral.NotRunLane("C. cloud ChatGPT/Codex-style assistant", "Cloud comparison must be run with a real prompt/session and pasted evidence; not faked by LocalGPT.",logger));
                    taskResult.Lanes.Add(CouncilChatStaticsGeneral.BuildManualExpectedLane(task,logger));
                    result.Tasks.Add(taskResult);
                }

                result.CompletedAtUtc = DateTime.UtcNow;
                if (request.SaveToKnowledge)
                    result.KnowledgeEntryId = await SaveBenchmarkKnowledgeAsync(result, cancellationToken);

                logger.LogInformation("Engineering benchmark {RunId} completed with {TaskCount} task(s).", result.RunId, result.Tasks.Count);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RunAsync request {request.ToString()}");
                return null;
            }
        }
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
                }, cancellationToken);

                return entry.Id;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Outer Error in ValidateBuildableArtifactAsync result {result.ToString()}");
                return null;
            }
        }
        private async Task<EngineeringBenchmarkLaneResult?> RunLocalGptLaneAsync(
            GlobalVariableSlopCollectionToRemove.BenchmarkTaskDefinition task,
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
                    MaxOutputTokens = 1024,
                    MaxContextTokens = 2048,
                    MaxRounds = 0
                };

                var artifacts = await artifactService.CreateImplementationArtifactsAsync(councilRequest, councilResult, cancellationToken);
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
                    .SelectMany(filter => CouncilChatStaticsGeneral.ReadZipEntriesSafe(filter,logger))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                lane.Evidence.AddRange(artifacts.Select(artifact => $"{artifact.Kind}: {artifact.Name} -> {artifact.DownloadUrl}"));
                lane.MissingFiles.AddRange(task.RequiredArtifactEntries.Where(required => !CouncilChatStringFunctions.ContainsZipEntry(artifactEntries, required, logger)));
                lane.MissingFilesScore = lane.MissingFiles.Count == 0 ? 10 : Math.Max(0, 10 - lane.MissingFiles.Count * 2);
                lane.ValidArchitectureScore = CouncilChatStaticsGeneral.ScoreArchitecture(task, artifactEntries, artifacts, logger);
                if (request.ValidateBuildableArtifacts)
                    lane.BuildChecks.AddRange(await CouncilChatStaticsGeneral.ValidateBuildableArtifactsAsync(artifacts, request.MaxBuildArtifacts, cancellationToken,logger).ConfigureAwait(false));

                lane.BuildabilityScore = CouncilChatStaticsGeneral.ScoreBuildability(task, artifacts, lane.BuildChecks, request.ValidateBuildableArtifacts, logger);
                lane.WrongPackagesTemplatesScore = CouncilChatStaticsGeneral.ScoreWrongTemplateRisk(task, artifactEntries, logger);
                lane.TotalScore = CouncilChatStaticsGeneral.SumScores(lane, logger);
                lane.Notes = lane.MissingFiles.Count == 0
                    ? "Deterministic LocalGPT artifact path produced expected benchmark files."
                    : "Artifact was produced, but required benchmark entries were missing. This is improvement fuel, not a pass.";
                if (request.ValidateBuildableArtifacts && lane.BuildChecks.Count > 0)
                    lane.Notes += $" Build checks: {string.Join("; ", lane.BuildChecks.Select(check => $"{check.ArtifactName}={check.Status}"))}.";
                return lane;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RunLocalGptLaneAsync task {task.ToString()} request {request.ToString()} ");
                return null;
            }
         
        }
    }
}
