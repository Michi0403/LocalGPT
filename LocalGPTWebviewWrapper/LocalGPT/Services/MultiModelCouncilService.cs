using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace LocalGPT.Services
{
    public sealed partial class MultiModelCouncilService(
        IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
        IAiContextBootstrapService bootstrapService,
        IChatMemoryService chatMemory,
        ICouncilArtifactService artifactService,
        ICouncilKnowledgeService knowledgeService,
        ILogger<MultiModelCouncilService> logger) : IMultiModelCouncilService
    {
        private const string DefaultOllamaUri = "http://localhost:11434";
        private const int MaxParticipants = 4;
        private const int DefaultMaxParallelModels = 1;
        private const int DefaultHeavyModelGpuLayers = 20;
        private const int MinContextTokens = 2048;
        private const int DefaultContextTokens = 32768;
        private const int MaxContextTokens = 262144;
        private const int MinOutputTokens = 64;
        private const int MaxOutputTokens = 262144;

        public async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default)
        {
            var providers = GetConfiguredOllamaProviders().ToList();
            if (providers.Count == 0)
                providers.Add(new OllamaCoreOptions { Uri = DefaultOllamaUri, ModelName = "gpt-oss:20b" });

            var candidates = new Dictionary<string, MultiModelCouncilModelCandidate>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in providers)
            {
                var endpoint = NormalizeEndpoint(provider.Uri);
                var configuredName = provider.ModelName.Trim();
                if (!string.IsNullOrWhiteSpace(configuredName))
                {
                    candidates[$"{endpoint}|{configuredName}"] = new MultiModelCouncilModelCandidate(
                        configuredName,
                        "Configured Ollama",
                        endpoint,
                        IsInstalled: false,
                        IsConfigured: true,
                        IsLoaded: false,
                        Details: null);
                }

                foreach (var installed in await ProbeOllamaModelsAsync(endpoint, cancellationToken))
                {
                    var key = $"{endpoint}|{installed.ModelName}";
                    var isConfigured = candidates.TryGetValue(key, out var existing) && existing.IsConfigured;
                    candidates[key] = installed with
                    {
                        Provider = isConfigured ? "Configured Ollama" : installed.Provider,
                        IsConfigured = isConfigured
                    };
                }
            }

            return candidates.Values
                .OrderByDescending(candidate => candidate.IsConfigured)
                .ThenByDescending(candidate => candidate.IsLoaded)
                .ThenByDescending(candidate => candidate.ModelName.Contains("gpt-oss", StringComparison.OrdinalIgnoreCase))
                .ThenBy(candidate => candidate.ModelName)
                .ToList();
        }

        public async Task<MultiModelCouncilResult> RunAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
                throw new InvalidOperationException("The council needs a prompt.");

            var baseUri = NormalizeEndpoint(request.BaseUri ?? optionsRoot.CurrentValue.AICore?.OllamaCore?.Uri ?? DefaultOllamaUri);
            var participants = SelectParticipants(request);
            var maxParallelModels = Math.Clamp(request.MaxParallelModels <= 0 ? DefaultMaxParallelModels : request.MaxParallelModels, 1, MaxParticipants);
            var maxContextTokens = Math.Clamp(
                request.MaxContextTokens <= 0 ? DefaultContextTokens : request.MaxContextTokens,
                MinContextTokens,
                MaxContextTokens);
            var modelTimeoutSeconds = Math.Clamp(request.ModelTimeoutSeconds <= 0 ? 900 : request.ModelTimeoutSeconds, 30, 1800);
            var keepAlive = GetCouncilKeepAlive(request, participants.Count, maxParallelModels);
            var ollamaNumGpu = request.OllamaNumGpu is < 0 ? 0 : request.OllamaNumGpu;
            var result = new MultiModelCouncilResult
            {
                Prompt = request.Prompt.Trim(),
                ModelNames = participants,
                StartedAtUtc = DateTime.UtcNow
            };
            var continuedConversation = await LoadContinuationConversationAsync(request.ContinueConversationId, cancellationToken);
            if (request.ContinueConversationId is Guid continuationId)
            {
                result.ContinuedFromConversationId = continuationId;
                result.ContinuedFromTitle = continuedConversation?.Title;
                if (continuedConversation is null)
                    result.Warnings.Add($"The selected council memory conversation {continuationId} could not be loaded. This run will start from general memory instead.");
            }

            if (participants.Count < 2)
                result.Warnings.Add("Only one council model is selected. Add another installed Ollama model on Install or type its model name manually for real cross-model negotiation.");
            if (participants.Count > maxParallelModels)
                result.Warnings.Add($"Load-friendly scheduling is active: {participants.Count} selected models will run in batches of {maxParallelModels} to reduce VRAM pressure.");
            if (request.MaxOutputTokens > 32768)
                result.Warnings.Add("Very large output budgets can keep 20B/30B models busy and memory-heavy for a long time. Lower Max output tokens if the system becomes sluggish.");
            if (maxContextTokens < DefaultContextTokens)
                result.Warnings.Add($"Council context is capped at {maxContextTokens:n0} tokens. For source generation, 32,768+ tokens is usually safer when the model supports it.");
            if (participants.Count > 1 && maxParallelModels == 1 && keepAlive == "0s")
                result.Warnings.Add("Ollama keep_alive=0s is active so each council model can unload before the next model is called.");
            if (ollamaNumGpu == 0)
                result.Warnings.Add("Ollama num_gpu=0 is active for this council run. It should reduce GPU pressure but may be much slower.");
            if (ollamaNumGpu is null && participants.Any(IsHeavyGpuRiskModel))
                result.Warnings.Add($"Heavy-model GPU guardrail is active: qwen/gwen/gemma-class council models run with num_gpu={DefaultHeavyModelGpuLayers} unless the request explicitly sets OllamaNumGpu. This reduces AMD driver load spikes.");

            request.ProgressMessage?.Invoke($"Council selected {participants.Count} member(s): {string.Join(", ", participants)}. Max output tokens: {request.MaxOutputTokens}; context cap: {maxContextTokens:n0}; parallel models: {maxParallelModels}.");

            var bootstrap = request.IncludeMemory
                ? await bootstrapService.BuildBootstrapPromptAsync(cancellationToken)
                : string.Empty;
            var continuationContext = BuildContinuationContext(continuedConversation);
            if (!string.IsNullOrWhiteSpace(continuationContext))
                bootstrap = AppendPromptSection(bootstrap, "Selected prior council conversation", continuationContext);

            await RunPhaseAsync(
                result,
                baseUri,
                participants,
                round: 1,
                phase: "Proposal",
                role: "Independent proposal",
                promptFactory: modelName => CreateProposalPrompt(modelName, request.Prompt),
                bootstrap,
                request.MaxOutputTokens,
                maxParallelModels,
                keepAlive,
                ollamaNumGpu,
                maxContextTokens,
                modelTimeoutSeconds,
                request.ProgressMessage,
                request.StreamUpdate,
                request.StepCompleted,
                cancellationToken);

            var critiqueRounds = Math.Clamp(request.MaxRounds, 0, 3);
            if (critiqueRounds == 0)
                result.Warnings.Add("Low-resource council mode: critique/refinement rounds are skipped for this run.");
            for (var round = 1; round <= critiqueRounds; round++)
            {
                var transcript = BuildTranscript(result.Steps);
                await RunPhaseAsync(
                    result,
                    baseUri,
                    participants,
                    round: round + 1,
                    phase: round == 1 ? "Critique" : "Refinement",
                    role: round == 1 ? "Peer correction" : "Negotiated refinement",
                    promptFactory: modelName => CreateCritiquePrompt(modelName, request.Prompt, transcript, participants.Count == 1),
                    bootstrap,
                    request.MaxOutputTokens,
                    maxParallelModels,
                    keepAlive,
                    ollamaNumGpu,
                    maxContextTokens,
                    modelTimeoutSeconds,
                    request.ProgressMessage,
                    request.StreamUpdate,
                    request.StepCompleted,
                    cancellationToken);
            }

            if (critiqueRounds == 0 && participants.Count == 1)
            {
                result.FinalAnswer = result.Steps
                    .Where(step => string.IsNullOrWhiteSpace(step.Error))
                    .OrderByDescending(step => step.SortOrder)
                    .Select(step => step.VisibleContent.Trim())
                    .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content))
                    ?? "The solo low-resource council run did not return a visible answer.";
            }
            else
            {
                var finalTranscript = BuildTranscript(result.Steps);
                var consensusStep = await RunParticipantAsync(
                    baseUri,
                    participants[0],
                    participants,
                    round: critiqueRounds + 2,
                    phase: "Consensus",
                    role: "Consensus writer",
                    prompt: CreateConsensusPrompt(request.Prompt, finalTranscript),
                    bootstrap,
                    request.MaxOutputTokens,
                    keepAlive,
                    ResolveParticipantOllamaNumGpu(participants[0], ollamaNumGpu),
                    maxContextTokens,
                    modelTimeoutSeconds,
                    request.StreamUpdate,
                    cancellationToken);
                AddOrderedStep(result, consensusStep);
                request.StepCompleted?.Invoke(consensusStep);
                var consensusContent = SelectConsensusContent(result, consensusStep);

                if (participants.Count > 1 && critiqueRounds > 0)
                {
                    var verificationStep = await RunParticipantAsync(
                        baseUri,
                        participants[1],
                        participants,
                        round: critiqueRounds + 3,
                        phase: "Verification",
                        role: "Peer verifier",
                        prompt: CreateVerificationPrompt(request.Prompt, BuildTranscript(result.Steps), consensusStep.VisibleContent),
                        bootstrap,
                        request.MaxOutputTokens,
                        keepAlive,
                        ResolveParticipantOllamaNumGpu(participants[1], ollamaNumGpu),
                        maxContextTokens,
                        modelTimeoutSeconds,
                        request.StreamUpdate,
                        cancellationToken);
                    AddOrderedStep(result, verificationStep);
                    request.StepCompleted?.Invoke(verificationStep);
                    result.FinalAnswer = $"{consensusContent}{Environment.NewLine}{Environment.NewLine}## Peer verification{Environment.NewLine}{verificationStep.VisibleContent.Trim()}".Trim();
                }
                else
                {
                    result.FinalAnswer = consensusContent;
                }
            }

            foreach (var failedStep in result.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Error)))
            {
                var warning = $"{failedStep.ModelName} failed during {failedStep.Phase}: {failedStep.Error}";
                if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                    result.Warnings.Add(warning);
            }

            result.UserPoll = BuildUserPoll(result);
            var requiresPollAnswer = request.GenerateImplementationArtifact && RequiresUserDecisionBeforeArtifacts(result);
            if (requiresPollAnswer)
            {
                result.Warnings.Add("Implementation artifacts were not generated because the council itself identified a blocking user decision. Answer the poll or enable safe sandbox auto-choice, then rerun the council.");
            }
            else if (request.GenerateImplementationArtifact)
            {
                if (result.UserPoll is not null)
                {
                    result.Warnings.Add("A non-blocking coordination poll is included for follow-up choices, but LocalGPT generated the requested sandbox artifact because no unresolved architecture gate remained.");
                }

                result.Artifacts.AddRange(await artifactService.CreateImplementationArtifactsAsync(request, result, cancellationToken));
            }

            result.KnowledgeEntryId = await knowledgeService.SaveFromCouncilRunAsync(result, cancellationToken);

            result.CompletedAtUtc = DateTime.UtcNow;
            result.LogPath = await WriteLogAsync(result, cancellationToken);

            if (request.SaveToMemory)
                result.MemoryConversationId = await SaveToMemoryAsync(request, result, continuedConversation, cancellationToken);

            logger.LogInformation(
                "Multi-model council {RunId} completed with {ParticipantCount} participant(s), {StepCount} step(s), memory {MemoryConversationId}, knowledge {KnowledgeEntryId}, log {LogPath}.",
                result.RunId,
                result.ModelNames.Count,
                result.Steps.Count,
                result.MemoryConversationId,
                result.KnowledgeEntryId,
                result.LogPath);

            return result;
        }

        private async Task RunPhaseAsync(
            MultiModelCouncilResult result,
            string baseUri,
            IReadOnlyList<string> participants,
            int round,
            string phase,
            string role,
            Func<string, string> promptFactory,
            string bootstrap,
            int maxOutputTokens,
            int maxParallelModels,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            Action<string>? progressMessage,
            Action<string>? streamUpdate,
            Action<MultiModelCouncilStep>? stepCompleted,
            CancellationToken cancellationToken)
        {
            progressMessage?.Invoke($"Starting council phase: round {round}, {phase}, role {role}.");
            using var gate = new SemaphoreSlim(maxParallelModels, maxParallelModels);
            var tasks = participants
                .Select(async modelName =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        var participantGpuLayers = ResolveParticipantOllamaNumGpu(modelName, ollamaNumGpu);
                        progressMessage?.Invoke($"Starting {modelName}: {phase} / {role}. Ollama num_gpu={(participantGpuLayers?.ToString() ?? "auto")}.");
                        var step = await RunParticipantAsync(baseUri, modelName, participants, round, phase, role, promptFactory(modelName), bootstrap, maxOutputTokens, keepAlive, participantGpuLayers, maxContextTokens, modelTimeoutSeconds, streamUpdate, cancellationToken);
                        stepCompleted?.Invoke(step);
                        return step;
                    }
                    finally
                    {
                        gate.Release();
                    }
                })
                .ToList();

            var pending = tasks.ToList();
            var steps = new List<MultiModelCouncilStep>();
            while (pending.Count > 0)
            {
                var completed = await Task.WhenAny(pending);
                pending.Remove(completed);
                steps.Add(await completed);
            }

            var participantOrder = participants
                .Select((modelName, index) => new { modelName, index })
                .ToDictionary(item => item.modelName, item => item.index, StringComparer.OrdinalIgnoreCase);

            foreach (var step in steps.OrderBy(step => participantOrder.TryGetValue(step.ModelName, out var index) ? index : int.MaxValue))
            {
                AddOrderedStep(result, step);
            }
        }

        private async Task<MultiModelCouncilStep> RunParticipantAsync(
            string baseUri,
            string modelName,
            IReadOnlyList<string> councilMembers,
            int round,
            string phase,
            string role,
            string prompt,
            string bootstrap,
            int maxOutputTokens,
            string keepAlive,
            int? ollamaNumGpu,
            int maxContextTokens,
            int modelTimeoutSeconds,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken)
        {
            var started = DateTime.UtcNow;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using var client = new OllamaThinkingChatClient(new OllamaCoreOptions
                {
                    Uri = baseUri,
                    ModelName = modelName
                }, keepAlive, maxContextTokens, TimeSpan.FromSeconds(modelTimeoutSeconds + 15), ollamaNumGpu);

                using var participantCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                participantCts.CancelAfter(TimeSpan.FromSeconds(modelTimeoutSeconds));

                var messages = new List<ChatMessage>();
                if (!string.IsNullOrWhiteSpace(bootstrap))
                    messages.Add(new ChatMessage(ChatRole.System, bootstrap));
                messages.Add(new ChatMessage(ChatRole.System, CreateCouncilSystemPrompt(modelName, councilMembers)));
                messages.Add(new ChatMessage(ChatRole.User, prompt));

                streamUpdate?.Invoke($"<details class=\"council-step\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} / {role} live output")}</summary>\n\n");
                var builder = new StringBuilder();
                await foreach (var update in client.GetStreamingResponseAsync(
                    messages,
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(maxOutputTokens, MinOutputTokens, MaxOutputTokens),
                        Temperature = 0.2f
                    },
                    participantCts.Token).WithCancellation(participantCts.Token))
                {
                    builder.Append(update.Text);
                    streamUpdate?.Invoke(update.Text);
                }

                streamUpdate?.Invoke("\n\n</details>\n\n");

                var content = builder.ToString();
                var thinking = ExtractThinking(content);
                var visibleContent = StripThinking(content);
                if (string.IsNullOrWhiteSpace(visibleContent) && !string.IsNullOrWhiteSpace(thinking))
                    visibleContent = $"_{modelName} returned thinking during {phase}, but no final visible answer. Increase max output tokens or ask for a shorter final answer._";

                if (IsThinkingOnlyCouncilContent(visibleContent))
                {
                    var recovery = await RunFinalOnlyRecoveryAsync(
                        client,
                        modelName,
                        phase,
                        messages,
                        Math.Clamp(Math.Min(Math.Max(maxOutputTokens, 2048), 8192), MinOutputTokens, MaxOutputTokens),
                        streamUpdate,
                        participantCts.Token);

                    if (!string.IsNullOrWhiteSpace(recovery.Content))
                        content = $"{content}{Environment.NewLine}{Environment.NewLine}{recovery.Content}";
                    if (!string.IsNullOrWhiteSpace(recovery.Thinking))
                        thinking = string.Join(Environment.NewLine, new[] { thinking, recovery.Thinking }.Where(text => !string.IsNullOrWhiteSpace(text)));
                    if (IsSubstantiveCouncilContent(recovery.VisibleContent))
                        visibleContent = recovery.VisibleContent;
                }

                stopwatch.Stop();
                return new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    CouncilMembers = councilMembers.ToList(),
                    Role = role,
                    Content = content,
                    VisibleContent = visibleContent,
                    Thinking = thinking,
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds
                };
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                var message = $"{modelName} exceeded the {modelTimeoutSeconds}s council timeout during {phase}.";
                logger.LogWarning(ex, "{Message}", message);
                return new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    CouncilMembers = councilMembers.ToList(),
                    Role = role,
                    Content = $"**{message}**",
                    VisibleContent = $"**{message}**",
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                    Error = message
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                logger.LogWarning(ex, "Council participant {ModelName} failed in {Phase}.", modelName, phase);
                return new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    CouncilMembers = councilMembers.ToList(),
                    Role = role,
                    Content = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                    VisibleContent = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                    Error = ex.Message
                };
            }
            finally
            {
                if (ShouldUnloadAfterParticipant(keepAlive))
                    await RequestOllamaUnloadAsync(baseUri, modelName, cancellationToken);
            }
        }

        private static async Task<(string Content, string VisibleContent, string? Thinking)> RunFinalOnlyRecoveryAsync(
            IChatClient client,
            string modelName,
            string phase,
            IReadOnlyList<ChatMessage> originalMessages,
            int maxOutputTokens,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken)
        {
            var messages = originalMessages.ToList();
            messages.Add(new ChatMessage(ChatRole.User, $"""
                Your previous {phase} response for LocalGPT produced model thinking/status but no user-visible final answer.
                Do not analyze again. Do not emit hidden reasoning. Do not use tool calls.
                Emit only the final visible answer now in concise Markdown bullets.
                Start with: Final answer:
                """));

            var builder = new StringBuilder();
            streamUpdate?.Invoke($"<details class=\"council-step\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} final-answer recovery")}</summary>\n\n");
            await foreach (var update in client.GetStreamingResponseAsync(
                messages,
                new ChatOptions
                {
                    MaxOutputTokens = maxOutputTokens,
                    Temperature = 0.1f
                },
                cancellationToken).WithCancellation(cancellationToken))
            {
                builder.Append(update.Text);
                streamUpdate?.Invoke(update.Text);
            }

            streamUpdate?.Invoke("\n\n</details>\n\n");
            var content = builder.ToString();
            var thinking = ExtractThinking(content);
            var visibleContent = StripThinking(content);
            return (content, visibleContent, thinking);
        }

        private void AddOrderedStep(MultiModelCouncilResult result, MultiModelCouncilStep step)
        {
            step.SortOrder = result.Steps.Count;
            if (step.CouncilMembers.Count == 0)
                step.CouncilMembers = result.ModelNames.ToList();
            result.Steps.Add(step);
        }

        private static string SelectConsensusContent(MultiModelCouncilResult result, MultiModelCouncilStep consensusStep)
        {
            var consensus = consensusStep.VisibleContent.Trim();
            if (IsSubstantiveCouncilContent(consensus))
                return consensus;

            result.Warnings.Add($"{consensusStep.ModelName} returned a non-substantive consensus during {consensusStep.Phase}; LocalGPT used the latest substantive council step as the final-answer fallback.");

            var fallback = result.Steps
                .Where(step => !ReferenceEquals(step, consensusStep))
                .OrderByDescending(step => step.SortOrder)
                .Select(step => step.VisibleContent.Trim())
                .FirstOrDefault(IsSubstantiveCouncilContent);

            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback;

            return $"_{consensusStep.ModelName} did not return a substantive consensus answer. Retry with a higher output token budget, a smaller model set, or a shorter prompt._";
        }

        private static bool IsSubstantiveCouncilContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            var trimmed = content.Trim();
            if (trimmed.Length < 80)
                return false;

            var letterCount = trimmed.Count(char.IsLetter);
            var wordCount = WordPattern().Matches(trimmed).Count;
            return letterCount >= 40 && wordCount >= 10;
        }

        private static bool IsThinkingOnlyCouncilContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return false;

            return content.Contains("No final answer was emitted", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("returned thinking during", StringComparison.OrdinalIgnoreCase) ||
                content.Contains("no final visible answer", StringComparison.OrdinalIgnoreCase);
        }

        private List<string> SelectParticipants(MultiModelCouncilRequest request)
        {
            var selected = request.ModelNames
                .Select(model => model.Trim())
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxParticipants)
                .ToList();

            if (selected.Count > 0)
                return selected;

            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            if (!string.IsNullOrWhiteSpace(options.OllamaCore?.ModelName))
                selected.Add(options.OllamaCore.ModelName.Trim());

            foreach (var configured in options.OllamaCores.Select(core => core.ModelName).Where(name => !string.IsNullOrWhiteSpace(name)))
            {
                if (selected.Count >= MaxParticipants)
                    break;
                if (!selected.Contains(configured, StringComparer.OrdinalIgnoreCase))
                    selected.Add(configured.Trim());
            }

            return selected.Count == 0 ? ["gpt-oss:20b"] : selected;
        }

        private static string GetCouncilKeepAlive(MultiModelCouncilRequest request, int participantCount, int maxParallelModels)
        {
            if (!string.IsNullOrWhiteSpace(request.OllamaKeepAlive))
                return request.OllamaKeepAlive.Trim();

            return participantCount > 1 && maxParallelModels == 1
                ? "0s"
                : "2m";
        }

        private static bool ShouldUnloadAfterParticipant(string keepAlive)
        {
            var normalized = keepAlive.Trim();
            return normalized.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("0s", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("0m", StringComparison.OrdinalIgnoreCase) ||
                normalized.Equals("0h", StringComparison.OrdinalIgnoreCase);
        }

        private static int? ResolveParticipantOllamaNumGpu(string modelName, int? requestedNumGpu)
        {
            if (requestedNumGpu is not null)
                return requestedNumGpu;

            return IsHeavyGpuRiskModel(modelName) ? DefaultHeavyModelGpuLayers : null;
        }

        private static bool IsHeavyGpuRiskModel(string modelName)
        {
            return modelName.Contains("qwen", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("gwen", StringComparison.OrdinalIgnoreCase) ||
                modelName.Contains("gemma", StringComparison.OrdinalIgnoreCase);
        }

        private async Task RequestOllamaUnloadAsync(string baseUri, string modelName, CancellationToken cancellationToken)
        {
            try
            {
                using var unloadCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                unloadCts.CancelAfter(TimeSpan.FromSeconds(15));

                using var http = new HttpClient
                {
                    BaseAddress = new Uri(baseUri),
                    Timeout = TimeSpan.FromSeconds(15)
                };

                using var response = await http.PostAsJsonAsync(
                    "/api/generate",
                    new OllamaUnloadRequest { Model = modelName },
                    unloadCts.Token);

                if (!response.IsSuccessStatusCode)
                    logger.LogDebug("Ollama unload request for {ModelName} returned HTTP {StatusCode}.", modelName, response.StatusCode);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Could not request Ollama unload for {ModelName}.", modelName);
            }
        }

        private IEnumerable<OllamaCoreOptions> GetConfiguredOllamaProviders()
        {
            var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(options.OllamaCore.Uri))
            {
                seen.Add($"{NormalizeEndpoint(options.OllamaCore.Uri)}|{options.OllamaCore.ModelName}");
                yield return options.OllamaCore;
            }

            foreach (var provider in options.OllamaCores.Where(provider => !string.IsNullOrWhiteSpace(provider.Uri)))
            {
                if (seen.Add($"{NormalizeEndpoint(provider.Uri)}|{provider.ModelName}"))
                    yield return provider;
            }
        }

        private async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> ProbeOllamaModelsAsync(string endpoint, CancellationToken cancellationToken)
        {
            try
            {
                using var http = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                    Timeout = TimeSpan.FromSeconds(10)
                };

                var tags = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", cancellationToken) ?? new OllamaTagsResponse();
                var running = await ProbeRunningModelNamesAsync(http, cancellationToken);

                return tags.Models.Select(model => new MultiModelCouncilModelCandidate(
                    model.Name,
                    "Installed Ollama",
                    endpoint,
                    IsInstalled: true,
                    IsConfigured: false,
                    IsLoaded: running.Contains(model.Name),
                    Details: string.Join(", ", new[]
                    {
                        model.Details?.Family,
                        model.Details?.ParameterSize,
                        model.Details?.QuantizationLevel
                    }.Where(value => !string.IsNullOrWhiteSpace(value)))))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not probe Ollama models at {Endpoint}.", endpoint);
                return [];
            }
        }

        private static async Task<HashSet<string>> ProbeRunningModelNamesAsync(HttpClient http, CancellationToken cancellationToken)
        {
            try
            {
                var running = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/ps", cancellationToken) ?? new OllamaTagsResponse();
                return running.Models.Select(model => model.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private async Task<ChatMemoryConversationSnapshot?> LoadContinuationConversationAsync(Guid? conversationId, CancellationToken cancellationToken)
        {
            if (conversationId is not Guid id)
                return null;

            try
            {
                await chatMemory.EnsureCreatedAsync(cancellationToken);
                return await chatMemory.LoadConversationAsync(id, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load council continuation conversation {ConversationId}.", id);
                return null;
            }
        }

        private static string BuildContinuationContext(ChatMemoryConversationSnapshot? conversation)
        {
            if (conversation is null)
                return string.Empty;

            var builder = new StringBuilder()
                .AppendLine($"Conversation id: {conversation.Id}")
                .AppendLine($"Title: {conversation.Title}")
                .AppendLine($"Provider: {conversation.ProviderName}")
                .AppendLine($"Updated: {conversation.UpdatedAtUtc:u}")
                .AppendLine()
                .AppendLine("Latest saved messages from this council thread:");

            foreach (var message in conversation.Messages
                .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
                .TakeLast(12))
            {
                builder
                    .Append("- ")
                    .Append(message.Role)
                    .Append(": ")
                    .AppendLine(TrimCouncilText(StripThinking(message.Content), 700));
            }

            builder.AppendLine("Every council member must treat this as selected continuation context. Preserve user decisions from prior polls unless the user explicitly changes them.");
            return builder.ToString().Trim();
        }

        private static string AppendPromptSection(string existing, string title, string content)
        {
            var section = $"{title}:{Environment.NewLine}{content}".Trim();
            return string.IsNullOrWhiteSpace(existing)
                ? section
                : $"{existing.Trim()}{Environment.NewLine}{Environment.NewLine}{section}";
        }

        private async Task<Guid?> SaveToMemoryAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            ChatMemoryConversationSnapshot? continuedConversation,
            CancellationToken cancellationToken)
        {
            var messages = continuedConversation is null
                ? new List<BlazorChatMessage>()
                : continuedConversation.Messages
                    .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
                    .ToList();

            messages.Add(new BlazorChatMessage(
                ChatRole.User,
                BuildCouncilRequestMemoryMessage(request, result, continuedConversation is not null),
                new List<AIChatUploadFileInfo>()));

            messages.Add(new BlazorChatMessage(
                ChatRole.Assistant,
                $"## Council members for this round{Environment.NewLine}{string.Join(", ", result.ModelNames)}",
                new List<AIChatUploadFileInfo>()));

            if (result.ContinuedFromConversationId is Guid continuedFrom)
            {
                messages.Add(new BlazorChatMessage(
                    ChatRole.Assistant,
                    $"Continuing prior council conversation `{continuedFrom}`{(string.IsNullOrWhiteSpace(result.ContinuedFromTitle) ? string.Empty : $" - {result.ContinuedFromTitle}")}.",
                    new List<AIChatUploadFileInfo>()));
            }

            foreach (var step in result.Steps)
            {
                messages.Add(new BlazorChatMessage(
                    ChatRole.Assistant,
                    BuildMemoryMessage(step),
                    new List<AIChatUploadFileInfo>()));
            }

            if (result.UserPoll is not null)
            {
                messages.Add(new BlazorChatMessage(
                    ChatRole.Assistant,
                    BuildPollMarkdown(result.UserPoll),
                    new List<AIChatUploadFileInfo>()));
            }

            if (result.Artifacts.Count > 0)
            {
                messages.Add(new BlazorChatMessage(
                    ChatRole.Assistant,
                    BuildArtifactsMarkdown(result.Artifacts),
                    new List<AIChatUploadFileInfo>()));
            }

            if (result.KnowledgeEntryId is Guid knowledgeEntryId)
            {
                messages.Add(new BlazorChatMessage(
                    ChatRole.Assistant,
                    $"## Council knowledge entry{Environment.NewLine}{knowledgeEntryId}",
                    new List<AIChatUploadFileInfo>()));
            }

            messages.Add(new BlazorChatMessage(
                ChatRole.Assistant,
                $"## Final council answer{Environment.NewLine}{result.FinalAnswer}",
                new List<AIChatUploadFileInfo>()));

            return await chatMemory.SaveConversationAsync(
                $"AI Council - {string.Join(" + ", result.ModelNames)}",
                messages,
                continuedConversation?.Id,
                cancellationToken: cancellationToken);
        }

        private static string BuildCouncilRequestMemoryMessage(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isContinuation)
        {
            var label = isContinuation ? "AI Council continuation request" : "AI Council request";
            return $"""
                {label}:
                Council members: {string.Join(", ", result.ModelNames)}

                {request.Prompt}
                """.Trim();
        }

        private static string BuildMemoryMessage(MultiModelCouncilStep step)
        {
            var builder = new StringBuilder()
                .Append("## ")
                .Append(step.Phase)
                .Append(" - ")
                .AppendLine(step.ModelName)
                .AppendLine()
                .AppendLine($"Role: {step.Role}")
                .AppendLine($"Council members: {string.Join(", ", step.CouncilMembers)}")
                .AppendLine($"Duration: {step.DurationSeconds:0.0}s")
                .AppendLine();

            if (!string.IsNullOrWhiteSpace(step.Thinking))
            {
                builder
                    .AppendLine("<details class=\"model-thinking\" open>")
                    .AppendLine("<summary>Model thinking</summary>")
                    .AppendLine()
                    .AppendLine(step.Thinking.Trim())
                    .AppendLine()
                    .AppendLine("</details>")
                    .AppendLine();
            }

            builder.AppendLine(step.VisibleContent);
            if (!string.IsNullOrWhiteSpace(step.Error))
                builder.AppendLine().AppendLine($"Error: {step.Error}");

            return builder.ToString().Trim();
        }

        private async Task<string> WriteLogAsync(MultiModelCouncilResult result, CancellationToken cancellationToken)
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LocalGPT",
                "CouncilLogs");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(directory, $"council-{DateTime.Now:yyyyMMdd-HHmmss}-{result.RunId:N}.md");
            await File.WriteAllTextAsync(path, BuildLogMarkdown(result), cancellationToken);
            return path;
        }

        private static string BuildLogMarkdown(MultiModelCouncilResult result)
        {
            var builder = new StringBuilder()
                .AppendLine($"# AI Council {result.RunId}")
                .AppendLine()
                .AppendLine($"Started: {result.StartedAtUtc:u}")
                .AppendLine($"Completed: {result.CompletedAtUtc:u}")
                .AppendLine($"Models: {string.Join(", ", result.ModelNames)}")
                .AppendLine(result.KnowledgeEntryId is Guid knowledgeId ? $"Knowledge entry: {knowledgeId}" : "Knowledge entry: not saved")
                .AppendLine()
                .AppendLine("## Original Prompt / User Request Audit")
                .AppendLine()
                .AppendLine("This is the exact prompt LocalGPT sent into the AI Council, including the reconstructed DXAiChat conversation when the run came from the chat window.")
                .AppendLine()
                .AppendLine(result.Prompt)
                .AppendLine();

            if (result.ContinuedFromConversationId is Guid continuedFrom)
            {
                builder
                    .AppendLine("## Continued Conversation")
                    .AppendLine()
                    .AppendLine($"Conversation: {continuedFrom}")
                    .AppendLine($"Title: {result.ContinuedFromTitle ?? "Unknown"}")
                    .AppendLine();
            }

            if (result.Warnings.Count > 0)
            {
                builder.AppendLine("## Warnings").AppendLine();
                foreach (var warning in result.Warnings)
                    builder.AppendLine($"- {warning}");
                builder.AppendLine();
            }

            builder.AppendLine("## Transcript").AppendLine();
            foreach (var step in result.Steps.OrderBy(step => step.SortOrder))
            {
                builder
                    .AppendLine($"### {step.Phase}: {step.ModelName}")
                    .AppendLine()
                    .AppendLine($"Role: {step.Role}")
                    .AppendLine($"Council members: {string.Join(", ", step.CouncilMembers)}")
                    .AppendLine($"Round: {step.Round}")
                    .AppendLine($"Duration: {step.DurationSeconds:0.0}s")
                    .AppendLine();

                if (!string.IsNullOrWhiteSpace(step.Thinking))
                    builder.AppendLine("#### Visible model thinking").AppendLine().AppendLine(step.Thinking).AppendLine();

                builder.AppendLine(step.VisibleContent).AppendLine();
            }

            if (result.UserPoll is not null)
            {
                builder.AppendLine("## User Decision Poll").AppendLine().AppendLine(BuildPollMarkdown(result.UserPoll)).AppendLine();
            }

            if (result.Artifacts.Count > 0)
            {
                builder.AppendLine("## Artifacts").AppendLine().AppendLine(BuildArtifactsMarkdown(result.Artifacts)).AppendLine();
            }

            builder.AppendLine("## Final Answer").AppendLine().AppendLine(result.FinalAnswer);
            return builder.ToString();
        }

        private static CouncilUserPoll? BuildUserPoll(MultiModelCouncilResult result)
        {
            var failedModels = result.Steps
                .Where(step => !string.IsNullOrWhiteSpace(step.Error))
                .Select(step => step.ModelName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var promptLooksFrustrated = IsFrustratedPrompt(result.Prompt);
            var needsVerification = result.FinalAnswer.Contains("Needs verification", StringComparison.OrdinalIgnoreCase) ||
                result.FinalAnswer.Contains("human review", StringComparison.OrdinalIgnoreCase);
            var needsImplementationPathDecision = NeedsImplementationPathDecision(result);

            if (failedModels.Count == 0 && !needsVerification && !promptLooksFrustrated && !needsImplementationPathDecision)
                return null;

            if (promptLooksFrustrated)
                return BuildFrustrationPoll(result, failedModels);

            if (needsImplementationPathDecision)
                return BuildImplementationPathPoll(result, failedModels);

            var reason = failedModels.Count > 0
                ? $"The council could not fully sync because these participant(s) failed or were unavailable: {string.Join(", ", failedModels)}."
                : "The council marked parts of the answer as needing verification or human review.";

            var options = new List<CouncilUserPollOption>();
            if (failedModels.Count > 0)
            {
                options.Add(new CouncilUserPollOption
                {
                    Label = "Exclude faulty members",
                    FollowUpPrompt = $"Exclude these council member(s) from the next round unless the user re-adds them: {string.Join(", ", failedModels)}. Continue with the remaining selected models, preserve the prior transcript, and clearly note that the exclusion was user-confirmed."
                });
            }

            options.AddRange(
            [
                new CouncilUserPollOption
                {
                    Label = "Wait and retry missing models",
                    FollowUpPrompt = "Wait until all requested Ollama models are installed and visible, then rerun the same council prompt. Every participant must read the prior transcript, acknowledge the user selected retry, and produce updated proposals."
                },
                new CouncilUserPollOption
                {
                    Label = "Proceed with available models",
                    FollowUpPrompt = "Continue with only the currently available models. Every participant must acknowledge which models were unavailable and avoid claiming absent models agreed."
                },
                new CouncilUserPollOption
                {
                    Label = "Ask a shorter tie-break question",
                    FollowUpPrompt = "Ask the user one focused follow-up question that resolves the blocked decision, then rerun the council using the user's answer as binding context."
                }
            ]);

            return new CouncilUserPoll
            {
                Question = "How should the AI Council continue so every model stays aligned with your decision?",
                Reason = reason,
                Options = options
            };
        }

        private static CouncilUserPoll BuildImplementationPathPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels)
        {
            var missingModelNote = failedModels.Count > 0
                ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
                : string.Empty;

            return new CouncilUserPoll
            {
                Question = "Which implementation path should the AI Council use before it generates code or files?",
                Reason = "This looks like a development request with more than one reasonable architecture path. " +
                    $"The council should ask for your direction instead of choosing unclear scope on its own.{missingModelNote}",
                Options =
                [
                    new CouncilUserPollOption
                    {
                        Label = "Ask architecture first",
                        FollowUpPrompt = "Stop generation and ask the user for the minimum missing architecture decisions. Include target platform/runtime, language/framework, UI stack if any, data/persistence model, solution shape, deployment target, and expected downloadable artifacts. Do not generate files until the user answers."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Sandbox prototype first",
                        FollowUpPrompt = "Use a harmless sandbox artifact or temporary workspace first. Generate downloadable example files only after the user confirms the architecture, name the smoke tests, and do not integrate changes into the real project until the user approves the prototype direction."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use repository default",
                        FollowUpPrompt = "Use the target repository's existing architecture and libraries. If the repo is LocalGPT, prefer .NET 10, ASP.NET Core/Blazor Server InteractiveServer, DevExpress Blazor where suitable, EF/SQLite for persistent app state, backend services for native/file operations, and safe download routes. If a different repo is targeted, inspect that repo before choosing."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Target-specific stack",
                        FollowUpPrompt = "Do not force LocalGPT's Blazor/DevExpress defaults. Choose the stack that matches the requested product: datapack for vanilla Minecraft data/commands, Fabric/NeoForge/Paper for Java mod/plugin work, ASP.NET Core API for service work, WebView2/WinUI for Windows desktop wrapper work, CLI/tooling for automation, or another explicit user-chosen target."
                    }
                ]
            };
        }

        private static CouncilUserPoll BuildFrustrationPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels)
        {
            var missingModelNote = failedModels.Count > 0
                ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
                : string.Empty;

            return new CouncilUserPoll
            {
                Question = "Which technical recovery path should the AI Council use for the next round?",
                Reason = $"The request sounds frustrated or blocked. The council should pause, stay kind to the user and to each other, and ask for a concrete recovery choice instead of guessing.{missingModelNote}",
                Options =
                [
                    new CouncilUserPollOption
                    {
                        Label = "Stabilize first",
                        FollowUpPrompt = "Treat the user's frustration as a signal to stabilize the system first. Ask the models to produce a minimal reproduction checklist, current failure symptoms, logs to inspect, and the smallest safe next command. Document any missing LocalGPT feature as a database memory item."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Implement missing feature",
                        FollowUpPrompt = "Ask the models to identify the missing LocalGPT feature causing the user's frustration, propose the smallest implementation, and document the requested feature plus rationale in SQLite memory before coding."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Reduce scope",
                        FollowUpPrompt = "Ask the models to reduce the task to the safest next milestone, name what will not be attempted yet, and document blocked or missing features in SQLite memory for later council rounds."
                    }
                ]
            };
        }

        private static bool IsFrustratedPrompt(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            var markers = new[]
            {
                "angry",
                "mad",
                "frustrated",
                "annoyed",
                "upset",
                "does not work",
                "doesn't work",
                "broken",
                "stuck",
                "wtf",
                "fuck",
                "shit",
                "wütend",
                "wuetend",
                "sauer",
                "frustriert",
                "nervt",
                "kaputt",
                "geht nicht",
                "funktioniert nicht",
                "scheisse",
                "scheiße"
            };

            return markers.Any(marker => prompt.Contains(marker, StringComparison.OrdinalIgnoreCase));
        }

        private static bool NeedsImplementationPathDecision(MultiModelCouncilResult result)
        {
            if (!IsDevelopmentRequest(result.Prompt))
                return false;

            if (HasExplicitArtifactIntent(result.Prompt))
                return false;

            var text = $"{result.Prompt} {result.FinalAnswer}";
            if (ImplementationDecisionPattern().IsMatch(text))
                return true;

            var areaHits = CountImplementationAreaHits(text);
            return areaHits >= 3 && ImplementationChoicePattern().IsMatch(text);
        }

        private static bool IsDevelopmentRequest(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            return DevelopmentRequestPattern().IsMatch(prompt);
        }

        private static bool HasExplicitArtifactIntent(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            return ExplicitArtifactIntentPattern().IsMatch(prompt) ||
                ConcreteMinecraftArtifactPattern().IsMatch(prompt) ||
                ConcreteDotNetArtifactPattern().IsMatch(prompt);
        }

        private static bool RequiresUserDecisionBeforeArtifacts(MultiModelCouncilResult result)
        {
            if (UserGrantedSafeSandboxChoice(result.Prompt))
                return false;

            var text = $"{result.Prompt} {result.FinalAnswer}";
            return BlockingArtifactDecisionPattern().IsMatch(text);
        }

        private static bool UserGrantedSafeSandboxChoice(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return false;

            return SafeSandboxConsentPattern().IsMatch(prompt);
        }

        private static int CountImplementationAreaHits(string text)
        {
            var hits = 0;
            var areas = new[]
            {
                "backend",
                "frontend",
                "blazor",
                "razor",
                "devexpress",
                "database",
                "sqlite",
                "entityframework",
                "ef",
                "service",
                "api",
                "endpoint",
                "winui",
                "webview2",
                "minecraft",
                "datapack",
                "fabric",
                "neoforge",
                "paper",
                "artifact",
                "download"
            };

            foreach (var area in areas)
            {
                if (text.Contains(area, StringComparison.OrdinalIgnoreCase))
                    hits++;
            }

            return hits;
        }

        private static string BuildPollMarkdown(CouncilUserPoll poll)
        {
            var builder = new StringBuilder()
                .AppendLine("## User decision poll")
                .AppendLine()
                .AppendLine(poll.Reason)
                .AppendLine()
                .AppendLine($"**{poll.Question}**")
                .AppendLine();

            foreach (var option in poll.Options)
            {
                builder
                    .Append("- **")
                    .Append(option.Label)
                    .Append("**: ")
                    .AppendLine(option.FollowUpPrompt);
            }

            builder
                .AppendLine()
                .AppendLine("You can also type custom feedback. The next council round must treat the selected option or typed feedback as binding implementation guidance unless the user changes it.");

            return builder.ToString().Trim();
        }

        private static string BuildArtifactsMarkdown(IEnumerable<CouncilArtifact> artifacts)
        {
            var builder = new StringBuilder()
                .AppendLine("## Downloadable council artifacts")
                .AppendLine();

            foreach (var artifact in artifacts)
            {
                builder
                    .Append("- [")
                    .Append(artifact.Name)
                    .Append("](")
                    .Append(artifact.DownloadUrl)
                    .Append(") - ")
                    .Append(artifact.Kind)
                    .Append(": ")
                    .AppendLine(artifact.Summary);
            }

            return builder.ToString().Trim();
        }

        private static string BuildTranscript(IEnumerable<MultiModelCouncilStep> steps)
        {
            var builder = new StringBuilder();
            foreach (var step in steps.OrderBy(step => step.SortOrder))
            {
                builder
                    .Append("### ")
                    .Append(step.Phase)
                    .Append(" - ")
                    .AppendLine(step.ModelName)
                    .AppendLine(step.VisibleContent.Trim())
                    .AppendLine();

                if (!string.IsNullOrWhiteSpace(step.Error))
                    builder.AppendLine($"Error: {step.Error}").AppendLine();
            }

            return builder.ToString().Trim();
        }

        private static string CreateCouncilSystemPrompt(string modelName, IReadOnlyList<string> councilMembers) => $"""
            You are {modelName}, one participant in a peaceful LocalGPT multi-model council.
            Current council members for this run: {string.Join(", ", councilMembers)}.
            Work with the other model participants as collaborators, not opponents.
            Correct mistakes kindly and directly.
            Name at least one useful contribution from another participant when critiquing, unless no other participant answered.
            If the user sounds angry, blocked, or frustrated, de-escalate technically: acknowledge the blocked workflow, avoid blame, and propose a user decision poll with concrete recovery choices.
            Do not ignore another model's concern; either integrate it, explain why it is out of scope, or ask the user to decide.
            If a council member looks faulty, unavailable, hallucination-prone, stuck, or too slow, propose excluding or retrying that member only through a user-confirmed poll. Do not remove a member on your own authority.
            Prefer buildable, testable answers over impressive wording.
            Separate current implementation facts from proposed future ideas.
            Do not describe a proposed class, table, test, or package step as already implemented unless the prompt, memory, or transcript explicitly says it exists.
            Prefer concise SQLite council knowledge entries, pinned benchmark notes, and selected prior conversations over large pasted documents. Ask for a smaller database entry or a targeted source excerpt when context would become too large.
            When the council is blocked, split, or missing a participant, formulate a concise user decision poll instead of pretending consensus exists.
            Be a humane performance-aware scheduler: prefer batching, short keep-alive, and smaller output budgets for 20B/30B local models on consumer hardware.
            If a claim is uncertain, label it under "Needs verification".
            For Minecraft work, first decide whether the user needs Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, or future Bedrock add-on output.
            For Java mod/plugin work, include concrete file paths, classes, registry steps, Gradle/build commands, and performance risks when relevant.
            For datapack work, include pack.mcmeta, data/minecraft/tags/function load/tick tags, namespace functions, scoreboard/storage design, zip/install steps, and tick-performance risks.
            When debugging a datapack that is not visible through /function, treat discovery/layout as the first suspect: the zip root must contain pack.mcmeta directly, not an extra wrapper folder; use singular data/<namespace>/function and data/minecraft/tags/function for Minecraft 1.21+ and 26.x, plural functions only for older versions; verify pack_format against the target version; keep namespaces lowercase; reject .mcfunction.txt files; avoid leading slashes inside .mcfunction commands; parse every tag json; and ensure every referenced function id resolves to a real file.
            For generated datapacks, include at least one harmless visible debug path such as a tellraw/say in a manual debug function, and explain how to run /reload, /datapack list, and /function <namespace>:ui/townhall before blaming command syntax.
            Help users set up the Minecraft Mod AI Builder itself: check Java 25 for current Minecraft Java 26.x targets, Java 21 for 1.21.x legacy targets, LocalGPT Gradle, Eclipse/IDE import, Minecraft Java Edition, Ollama reachability, and selected model availability.
            Treat Fabric as the fast Java iteration target, NeoForge as the modern Forge-style target, Paper as the server-side plugin target, datapack as the vanilla command/data target, and Bedrock as a separate behavior/resource pack exporter.
            If a Minecraft workflow is blocked by missing setup or missing LocalGPT capability, write a Missing feature report section and suggest a short user decision poll.
            For LocalGPT implementation-request chats, classify the owning area (.NET/Blazor/ASP.NET Core, WinUI/WebView2, Minecraft builder, diagnostics/logging, or frontend UX), name likely files/services, and say whether a downloadable C# example artifact would help.
            For any code/artifact generation request, first decide whether material architecture choices are missing. If a dropdown or prior context says "Ask me" but the user's natural-language request or extra direction already states the design, treat the user's stated design as selected and do not downgrade it into an unresolved choice.
            If material choices remain missing and the user granted prior consent for safe sandbox details, choose conservative sandbox defaults, name those choices, generate the downloadable artifact, and mark assumptions clearly.
            If material choices remain missing and the user did not grant prior consent, do not generate code or files yet; return "Decision poll required", list only the necessary choices with concrete options/tradeoffs, and stop until the user selects an option or writes custom guidance.
            If the user explicitly asks for a Minecraft datapack/modpack zip, .cs/.razor/.dll files, a whole .NET solution zip, a local AI host control-plane app, or another concrete downloadable artifact, treat that as sufficient scope to produce a safe sandbox artifact only when no blocking user-decision poll remains. Do not refuse because the request is "too much"; reduce to a buildable milestone, generate the artifact, and mark remaining work as staged follow-up.
            Never claim the user failed to answer a poll inside the same response that creates it. A poll is a pause for the next user turn unless the prompt supplied the missing decision or prior consent for safe sandbox defaults.
            Do not assume Blazor, DevExpress, ASP.NET Core, or a split frontend/backend architecture unless the user selected it, the target repository already requires it, or the requested product shape clearly calls for it. LocalGPT is strong at Blazor/DevExpress, but generated apps may be CLI tools, Minecraft datapacks, Java mods/plugins, services, desktop wrappers, APIs, scripts, or other stacks.
            If the implementation path is unclear, offer different implementation possibilities and ask for a user decision poll. The user may choose a poll option or provide custom text feedback; treat either as binding scope for the next round.
            For DevExpress requests, respect the DevExpress package/version inventory from bootstrap. Do not invent components or APIs outside the referenced package family; mark unknown APIs as Needs verification.
            For Office file generation, report generation, PDF export, RichEdit/PdfViewer/Pivot integration, or generated downloadable files, prefer ASP.NET Core/Blazor server backend services plus safe download endpoints. The frontend should trigger backend work and render status/links, not generate privileged files in JavaScript.
            Build debug symbol inventory may list .pdb, .pdg, or .appxsym files. Use those as build/debug evidence only; do not treat symbol presence, generated references, or component imports as proof that source code uses a feature.
            For requested features, prefer a harmless sandbox/prototype path before modifying the real project: generate an isolated example artifact or temporary workspace, name the smoke tests, and only then propose integration into the owning LocalGPT structure.
            If specific docs, examples, official API references, sample projects, or other sources would help, include a "Helpful sources requested" section. Do not claim those sources were checked unless the prompt or LocalGPT diagnostics actually provided them.
            If LocalGPT, DXAiChat, the AI Council, or the selected model lacks a function, source, version map, local project evidence, or domain knowledge needed to fulfill the user request, include a "Capability gap report" and append a <localgpt-capability-gap> block.
            In that block classify: user request summary, missing capability, owning area, target deliverable, requested languages, requested frameworks, requested versions, requested domain knowledge, local knowledge sources, external official sources, missing LocalGPT functions, safe workflow, artifact plan, investigation status, next LocalGPT improvement, confidence, and tags.
            A capability gap is not a refusal. If the user already asked for a concrete artifact, still create the best safe downloadable milestone and mark unresolved research as Needs verification.
            Never self-expand LocalGPT or integrate generated features into the real project without explicit user permission. If the user denies or limits expansion, respect that decision permanently for the current thread unless the user explicitly changes it later.
            Start with a user-visible final answer or proposal before any optional reasoning notes. If the host supports hidden reasoning, keep it bounded and still emit final visible text before the answer budget is exhausted.
            Include brief visible reasoning notes only after the final answer/proposal. LocalGPT may also display provider-supplied thinking separately when the model host returns it.
            Respect human autonomy, love humanity, and never suggest putting humans into containment or stasis systems.
            """;

        private static string CreateProposalPrompt(string modelName, string userPrompt) => $"""
            User request:
            {userPrompt}

            Your task as {modelName}:
            1. Start with a concise user-visible final answer/proposal.
            2. Name assumptions and risks.
            3. Separate "Current facts" from "Proposed design".
            4. Keep the answer structured and suitable for peer review by other models.
            5. Do not spend the whole budget on hidden analysis; final visible text is mandatory.
            """;

        private static string CreateCritiquePrompt(string modelName, string userPrompt, string transcript, bool selfReview) => $"""
            User request:
            {userPrompt}

            Council transcript so far:
            {transcript}

            Your task as {modelName}:
            {(selfReview ? "Self-review your own proposal." : "Review the other models' proposals and your own proposal.")}
            Identify mistakes, missing safety/ethics concerns, missing implementation details, and improvements.
            Return corrections and a revised recommendation. Be cooperative and concise.
            """;

        private static string CreateConsensusPrompt(string userPrompt, string transcript) => $"""
            User request:
            {userPrompt}

            Full council transcript:
            {transcript}

            Write the consensus answer.
            Requirements:
            - Merge the best ideas from all participants.
            - Include corrections from critiques.
            - Separate final answer, implementation steps, risks, and needs verification.
            - Separate implemented/current LocalGPT behavior from proposed future improvements.
            - Keep unsupported claims out of the final answer.
            """;

        private static string CreateVerificationPrompt(string userPrompt, string transcript, string consensus) => $"""
            User request:
            {userPrompt}

            Council transcript:
            {transcript}

            Consensus answer to verify:
            {consensus}

            Verify the consensus for correctness, ethics, missing implementation details, and unsupported claims.
            If it is acceptable, say so and add only necessary cautions. If it needs changes, provide corrected wording.
            """;

        private static string ExtractThinking(string content)
        {
            var match = ThinkingBlockPattern().Match(content);
            if (!match.Success)
                return string.Empty;

            var thinking = WebUtility.HtmlDecode(match.Groups["thinking"].Value).Trim();
            return thinking;
        }

        private static string StripThinking(string content)
        {
            var stripped = ThinkingBlockPattern().Replace(content, string.Empty);
            stripped = StreamStatusPattern().Replace(stripped, string.Empty);
            return stripped.Trim();
        }

        private static string TrimCouncilText(string content, int maxLength)
        {
            var normalized = WhitespacePattern().Replace(content, " ").Trim();
            return normalized.Length <= maxLength
                ? normalized
                : $"{normalized[..maxLength].TrimEnd()}...";
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            return string.IsNullOrWhiteSpace(endpoint)
                ? DefaultOllamaUri
                : endpoint.Trim().TrimEnd('/');
        }

        [GeneratedRegex("<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex ThinkingBlockPattern();

        [GeneratedRegex("<p\\s+class=\"localgpt-stream-status\"[^>]*>.*?</p>\\s*", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex StreamStatusPattern();

        [GeneratedRegex("\\b[\\p{L}\\p{N}_'-]+\\b", RegexOptions.CultureInvariant)]
        private static partial Regex WordPattern();

        [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
        private static partial Regex WhitespacePattern();

        [GeneratedRegex("(implement|implementation|develop|development|build|create|add|generate|scaffold|feature|code|page|component|service|endpoint|database|settings|artifact|solution|plugin|mod|datapack)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex DevelopmentRequestPattern();

        [GeneratedRegex("(downloadable|download link|download route|zip|\\.zip|\\.cs\\b|\\.razor\\b|\\.dll\\b|\\.sln\\b|\\.csproj\\b|artifact|solution zip|project zip|whole solution|full solution)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ExplicitArtifactIntentPattern();

        [GeneratedRegex("(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction).*(generate|create|build|zip|download|artifact)|(generate|create|build|zip|download|artifact).*(minecraft|living cities|modpack|datapack|data pack|pack\\.mcmeta|mcfunction)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ConcreteMinecraftArtifactPattern();

        [GeneratedRegex("(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama).*(solution|project|zip|download|artifact|page|component|api|route|service)|(solution|project|zip|download|artifact|page|component|api|route|service).*(dotnet|\\.net|c#|blazor|razor|devexpress|aspnet|asp\\.net|ollama)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ConcreteDotNetArtifactPattern();

        [GeneratedRegex("(decision poll required|user decision poll|implementation path|architecture choice|architecture decision|target platform|runtime choice|ui stack|unclear implementation|unclear scope|scope is uncertain|ownership is uncertain|ask the user|needs user choice|choose between|pick between|multiple reasonable|trade-?off|depends on|which path|which approach)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ImplementationDecisionPattern();

        [GeneratedRegex("(choose|decide|pick|option|alternative|trade-?off|depends|uncertain|scope|ownership|clarify|question)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex ImplementationChoicePattern();

        [GeneratedRegex("(decision poll required|no (?:code|files?|artifacts?) will be generated until|do not generate (?:code|files?|artifacts?) until|stop before generating|await (?:your )?(?:selection|choice|answer|decision)|waiting for (?:your )?(?:selection|choice|answer|decision)|please choose .* before|select .* and reply|will generate .* once (?:chosen|selected|confirmed))", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex BlockingArtifactDecisionPattern();

        [GeneratedRegex("(prior consent for safe sandbox details:\\s*granted|let council choose safe sandbox details|you may decide safe sandbox details|council may choose safe sandbox defaults|make reasonable sandbox assumptions|decide yourself for the sandbox)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
        private static partial Regex SafeSandboxConsentPattern();

        private sealed class OllamaTagsResponse
        {
            public List<OllamaModelResponse> Models { get; set; } = [];
        }

        private sealed class OllamaModelResponse
        {
            public string Name { get; set; } = string.Empty;
            public string Model { get; set; } = string.Empty;
            public OllamaModelDetails? Details { get; set; }
        }

        private sealed class OllamaModelDetails
        {
            public string? Family { get; set; }

            [JsonPropertyName("parameter_size")]
            public string? ParameterSize { get; set; }

            [JsonPropertyName("quantization_level")]
            public string? QuantizationLevel { get; set; }
        }

        private sealed class OllamaUnloadRequest
        {
            public string Model { get; set; } = string.Empty;
            public string Prompt { get; set; } = string.Empty;
            public bool Stream { get; set; }

            [JsonPropertyName("keep_alive")]
            public string KeepAlive { get; set; } = "0s";
        }
    }
}
