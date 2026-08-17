using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates provider model benchmark behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class ProviderModelBenchmarkService
    {
    /// <summary>
    /// Builds tasks as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="request">Benchmark request whose optional caller-supplied task definitions take precedence over the maintained standalone suite.</param>
    /// <returns>The collection produced by the operation.</returns>
    private IReadOnlyList<BenchmarkTask> BuildTasks(ProviderModelBenchmarkRequest request) {
    try
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TaskDefinitions.Count > 0)
        {
            return request.TaskDefinitions
                .Where(task => !string.IsNullOrWhiteSpace(task.Name) && !string.IsNullOrWhiteSpace(task.Prompt))
                .Select(task => new BenchmarkTask(
                    task.Name.Trim(),
                    task.Prompt.Trim(),
                    task.ExpectedTokens.Where(token => !string.IsNullOrWhiteSpace(token)).Select(token => token.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
                    task.ExpectJson,
                    Math.Clamp(task.ExpectedSectionCount, 0, 16),
                    task.RequireEmbeddedJsonObject,
                    task.EnforceRoleExecution))
                .ToList();
        }

        return [
        new("C# correctness", "A C# loop sums integers 1 through 5 but uses `for (var i = 1; i < 5; i++)`. State the bug and corrected loop in two short lines.", ["<= 5", "off-by-one"]),
        new("Provider identity", "Explain in one sentence why the pair (provider endpoint, model name) is safer as an AI model address than model name alone.", ["provider", "model"]),
        new("Structured settings", "Return JSON with keys contextTokens, outputTokens, parallelModels and reason for a conservative local AI benchmark configuration.", ["contextTokens", "outputTokens", "parallelModels"], ExpectJson: true),
        new("Accessibility", "Give three concise accessibility requirements for a reusable interactive model card containing select, properties and benchmark actions.", ["keyboard", "label", "focus"])
    ];
    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(BuildTasks)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(BuildTasks)} failed.");
        throw;
    }
}

    /// <summary>
    /// Builds profiles as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="model">Provider-qualified target whose provider capabilities influence adaptive control profiles.</param>
    /// <param name="request">Benchmark request containing the selected profile-generation policy and lower token bounds.</param>
    /// <param name="profileCount">Number of generated profile points requested for this target.</param>
    /// <param name="maximumContext">Inclusive context-token endpoint used by generated profiles.</param>
    /// <param name="maximumOutput">Inclusive output-token endpoint used by generated profiles.</param>
    /// <returns>Ordered benchmark profiles that preserve adaptive legacy behavior or evenly divide the requested token interval.</returns>
    private IReadOnlyList<BenchmarkProfile> BuildProfiles(
        ProviderModelReference model,
        ProviderModelBenchmarkRequest request,
        int profileCount,
        int maximumContext,
        int maximumOutput)
    {
    try
    {
            var profiles = new List<BenchmarkProfile>();
            void Add(string name, int context, int output, int? numGpu = null)
            {
                context = Math.Clamp(context, 2048, maximumContext);
                output = Math.Clamp(output, 128, maximumOutput);
                if (profiles.Any(item => item.ContextTokens == context
                    && item.OutputTokens == output
                    && item.OllamaNumGpu == numGpu))
                    return;
                profiles.Add(new BenchmarkProfile(name, context, output, numGpu));
            }

            if (request.ProfileMode == ProviderModelBenchmarkProfileMode.EvenlySpaced)
            {
                var minimumContext = Math.Clamp(request.MinimumContextTokens, 2048, maximumContext);
                var minimumOutput = Math.Clamp(request.MinimumOutputTokens, 128, maximumOutput);
                var steps = Math.Max(1, profileCount);
                for (var index = 0; index < steps; index++)
                {
                    var ratio = steps == 1 ? 1d : index / (double)(steps - 1);
                    var context = Math.Clamp(
                        (int)Math.Round(minimumContext + ((maximumContext - minimumContext) * ratio)),
                        minimumContext,
                        maximumContext);
                    var output = Math.Clamp(
                        (int)Math.Round(minimumOutput + ((maximumOutput - minimumOutput) * ratio)),
                        minimumOutput,
                        maximumOutput);
                    Add($"Even step {index + 1}/{steps}", context, output);
                }

                return profiles;
            }

            Add("Low latency", Math.Min(2048, maximumContext), Math.Min(256, maximumOutput));
            Add("Balanced", Math.Min(4096, maximumContext), Math.Min(512, maximumOutput));
            if (request.IncludeCpuSafeControl &&
                model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
            {
                Add("CPU-safe control", Math.Min(4096, maximumContext), Math.Min(512, maximumOutput), 0);
            }
            Add("Quality", Math.Min(8192, maximumContext), Math.Min(768, maximumOutput));
            Add("Maximum bounded", maximumContext, maximumOutput);

            return profiles.Take(profileCount).ToList();

    }
    catch (Exception __serviceMethodException)
    {
        if (__serviceMethodException is OperationCanceledException)
            logger.LogDebug(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(BuildProfiles)} was canceled.");
        else
            logger.LogError(__serviceMethodException, $"Service method {nameof(ProviderModelBenchmarkService)}.{nameof(BuildProfiles)} failed.");
        throw;
    }
}



    /// <summary>
    /// Performs score quality as part of the provider model benchmark service workflow, applying the service's runtime policy, state management, and diagnostics as required.
    /// </summary>
    /// <param name="response">Response value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <param name="task">Task value supplied to the provider model benchmark operation and used when producing its result.</param>
    /// <returns>The double produced by the operation.</returns>
    private double ScoreQuality(string response, BenchmarkTask task)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(response) || LooksLikeGenericCapabilityRefusal(response))
                return 0d;

            var matches = task.ExpectedTokens.Count(token => response.Contains(token, StringComparison.OrdinalIgnoreCase));
            if (task.ExpectedSectionCount > 0 || task.RequireEmbeddedJsonObject)
            {
                var tokenScore = task.ExpectedTokens.Count == 0 ? 1d : matches / (double)task.ExpectedTokens.Count;
                var sectionMatches = Enumerable.Range(1, task.ExpectedSectionCount)
                    .Count(index => response.Contains($"Task {index}", StringComparison.OrdinalIgnoreCase));
                var sectionScore = task.ExpectedSectionCount == 0 ? 1d : sectionMatches / (double)task.ExpectedSectionCount;
                JsonDocument? embeddedDocument = null;
                var jsonScore = !task.RequireEmbeddedJsonObject || TryParseFirstJsonObject(response, out embeddedDocument) ? 1d : 0d;
                embeddedDocument?.Dispose();
                return Math.Clamp(0.10d + (0.40d * tokenScore) + (0.30d * sectionScore) + (0.20d * jsonScore), 0d, 1d);
            }

            var score = 0.2d;
            score += task.ExpectedTokens.Count == 0 ? 0.6d : 0.6d * matches / task.ExpectedTokens.Count;
            if (task.ExpectJson)
            {
                if (TryParseFirstJsonObject(response, out var document))
                {
                    document?.Dispose();
                    score += 0.2d;
                }
            }
            else
            {
                score += 0.2d;
            }
            return Math.Clamp(score, 0d, 1d);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                logger.LogDebug(exception, "Scoring provider benchmark response was cancelled.");
            else
                logger.LogError(exception, "Scoring provider benchmark response failed; model output content was omitted.");
            throw;
        }
    }

    }
}
