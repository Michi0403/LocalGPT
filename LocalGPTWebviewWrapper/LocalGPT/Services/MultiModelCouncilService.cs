using DevExpress.AIIntegration.Blazor.Chat;
using DevExpress.Charts.Native;
using DevExpress.CodeParser;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Extensions.PlainStatics;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Services
{
    public sealed partial class MultiModelCouncilService(
        IOptionsMonitor<BusinessObjects.ConfigurationRoot> optionsRoot,
        IAiContextBootstrapService bootstrapService,
        IChatMemoryService chatMemory,
        ICouncilArtifactService artifactService,
        ICouncilKnowledgeService knowledgeService,
        IPromptConfigService promptConfigService,
        IChatResponseFormatterFactory formatterFactory,
        IChatProtocolResolver protocolResolver,
        ILogger<MultiModelCouncilService> logger) : IMultiModelCouncilService
    {
   

        public async Task<IReadOnlyList<MultiModelCouncilModelCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var providers = GetConfiguredOllamaProviders().ToList();
                if (providers.Count == 0)
                    providers.Add(new OllamaCoreOptions { Uri = DefaultOllamaUri, ModelName = "gpt-oss:20b" });

                var candidates = new Dictionary<string, MultiModelCouncilModelCandidate>(StringComparer.OrdinalIgnoreCase);

                foreach (var provider in providers)
                {
                    var endpoint = CouncilChatStringFunctions.MultiModelCouncilServiceNormalizeEndpoint(provider.Uri, logger);
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

                    foreach (var installed in await ProbeOllamaModelsAsync(endpoint, cancellationToken).ConfigureAwait(false))
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetCandidatesAsync");
                return new List<MultiModelCouncilModelCandidate>();
            }
        }

        public async Task<MultiModelCouncilResult?> RunAsync(MultiModelCouncilRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Prompt))
                    throw new InvalidOperationException("The council needs a prompt.");

                var baseUri = CouncilChatStringFunctions.MultiModelCouncilServiceNormalizeEndpoint(request.BaseUri ?? optionsRoot.CurrentValue.AICore?.OllamaCore?.Uri ?? DefaultOllamaUri, logger);
                var participants = SelectParticipants(request);
                var maxParallelModels = Math.Clamp(request.MaxParallelModels <= 0 ? DefaultMaxParallelModels : request.MaxParallelModels, 1, MaxParticipants);
                var maxContextTokens = Math.Clamp(
                    request.MaxContextTokens <= 0 ? DefaultContextTokens : request.MaxContextTokens,
                    MinContextTokens,
                    MaxContextTokens);
                var modelTimeoutSeconds = Math.Clamp(request.ModelTimeoutSeconds <= 0 ? 900 : request.ModelTimeoutSeconds, 30, 1800);
                var keepAlive = MultiModelCouncilServiceGetCouncilKeepAlive(request, participants.Count, maxParallelModels, logger);
                var ollamaNumGpu = request.OllamaNumGpu is < 0 ? 0 : request.OllamaNumGpu;
                var result = new MultiModelCouncilResult
                {
                    Prompt = request.Prompt.Trim(),
                    ModelNames = participants,
                    StartedAtUtc = DateTime.UtcNow
                };
                var continuedConversation = await LoadContinuationConversationAsync(request.ContinueConversationId, cancellationToken, logger).ConfigureAwait(false);
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
                if (maxContextTokens < 64000)
                    result.Warnings.Add($"Council context is capped at {maxContextTokens:n0} tokens. Values below 64K are quick-chat/diagnostic budgets, not valid source-generation acceptance tests.");
                if (participants.Count > 1 && maxParallelModels == 1 && keepAlive == "0s")
                    result.Warnings.Add("Ollama keep_alive=0s is active so each council model can unload before the next model is called.");
                if (ollamaNumGpu == 0)
                    result.Warnings.Add("Ollama num_gpu=0 is active for this council run. It should reduce GPU pressure but may be much slower.");
                if (ollamaNumGpu is null && participants.Any(filter => MultiModelCouncilServiceIsHeavyGpuRiskModel(filter,logger)))
                    result.Warnings.Add($"Heavy-model GPU guardrail is active: qwen/gwen/gemma-class council models run with num_gpu={DefaultHeavyModelGpuLayers} unless the request explicitly sets OllamaNumGpu. This reduces AMD driver load spikes.");

                request.ProgressMessage?.Invoke($"Council selected {participants.Count} member(s): {string.Join(", ", participants)}. Max output tokens: {request.MaxOutputTokens}; context cap: {maxContextTokens:n0}; parallel models: {maxParallelModels}.");

                var bootstrap = request.IncludeMemory
                    ? await bootstrapService.BuildBootstrapPromptAsync(cancellationToken).ConfigureAwait(false)
                    : string.Empty;
                var continuationContext = MultiModelCouncilServiceBuildContinuationContext(continuedConversation, logger);
                if (!string.IsNullOrWhiteSpace(continuationContext))
                    bootstrap = MultiModelCouncilServiceAppendPromptSection(bootstrap, "Selected prior council conversation", continuationContext, logger);

                await RunPhaseAsync(
                    result,
                    baseUri,
                    participants,
                    round: 1,
                    phase: "Proposal",
                    role: "Independent proposal",
                    promptFactory: modelName => CouncilChatStringFunctions.MultiModelCouncilServiceCreateProposalPrompt(modelName, request.Prompt, logger),
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
                    cancellationToken).ConfigureAwait(false);

                var critiqueRounds = Math.Clamp(request.MaxRounds, 0, 3);
                if (critiqueRounds == 0)
                    result.Warnings.Add("Low-resource council mode: critique/refinement rounds are skipped for this run.");
                for (var round = 1; round <= critiqueRounds; round++)
                {
                    var transcript = CouncilChatStringFunctions.MultiModelCouncilServiceBuildTranscript(result.Steps, logger);
                    await RunPhaseAsync(
                        result,
                        baseUri,
                        participants,
                        round: round + 1,
                        phase: round == 1 ? "Critique" : "Refinement",
                        role: round == 1 ? "Peer correction" : "Negotiated refinement",
                        promptFactory: modelName => CouncilChatStringFunctions.MultiModelCouncilServiceCreateCritiquePrompt(modelName, request.Prompt, transcript, participants.Count == 1, logger),
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
                        cancellationToken).ConfigureAwait(false);
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
                    var finalTranscript = CouncilChatStringFunctions.MultiModelCouncilServiceBuildTranscript(result.Steps, logger);
                    var consensusStep = await RunParticipantAsync(
                        baseUri,
                        participants[0],
                        participants,
                        round: critiqueRounds + 2,
                        phase: "Consensus",
                        role: "Consensus writer",
                        prompt: CouncilChatStringFunctions.MultiModelCouncilServiceCreateConsensusPrompt(request.Prompt, finalTranscript, logger),
                        bootstrap,
                        request.MaxOutputTokens,
                        keepAlive,
                        MultiModelCouncilServiceResolveParticipantOllamaNumGpu(participants[0], ollamaNumGpu, logger),
                        maxContextTokens,
                        modelTimeoutSeconds,
                        request.StreamUpdate,
                        cancellationToken).ConfigureAwait(false);
                    ArgumentNullException.ThrowIfNull(consensusStep);
                    MultiModelCouncilServiceAddOrderedStep(result, consensusStep,logger);
                    request.StepCompleted?.Invoke(consensusStep);
                    var consensusContent = MultiModelCouncilServiceSelectConsensusContent(result, consensusStep, logger);

                    if (participants.Count > 1 && critiqueRounds > 0)
                    {
                        var verificationStep = await RunParticipantAsync(
                            baseUri,
                            participants[1],
                            participants,
                            round: critiqueRounds + 3,
                            phase: "Verification",
                            role: "Peer verifier",
                            prompt: CouncilChatStringFunctions.MultiModelCouncilServiceCreateVerificationPrompt(request.Prompt, CouncilChatStringFunctions.MultiModelCouncilServiceBuildTranscript(result.Steps, logger), consensusStep.VisibleContent, logger),
                            bootstrap,
                            request.MaxOutputTokens,
                            keepAlive,
                            MultiModelCouncilServiceResolveParticipantOllamaNumGpu(participants[1], ollamaNumGpu, logger),
                            maxContextTokens,
                            modelTimeoutSeconds,
                            request.StreamUpdate,
                            cancellationToken);
                        ArgumentNullException.ThrowIfNull(verificationStep);
                        MultiModelCouncilServiceAddOrderedStep(result, verificationStep, logger);
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

                result.UserPoll = CouncilChatStaticsGeneral.MultiModelCouncilServiceBuildUserPoll(result, logger);
                var adviceOnlyPrompt = CouncilChatStaticsGeneral.IsAdviceOnlyPrompt(result.Prompt, logger) ?? false;
                var shouldGenerateArtifacts = request.GenerateImplementationArtifact &&
                    !adviceOnlyPrompt &&
                    CouncilChatStaticsGeneral.MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(result.Prompt, logger);
                var requiresPollAnswer = shouldGenerateArtifacts && CouncilChatStaticsGeneral.MultiModelCouncilServiceRequiresUserDecisionBeforeArtifacts(result, logger);
                if (requiresPollAnswer)
                {
                    result.Warnings.Add("Implementation artifacts were not generated because the council itself identified a blocking user decision. Answer the poll or enable safe sandbox auto-choice, then rerun the council.");
                }
                else if (shouldGenerateArtifacts)
                {
                    if (result.UserPoll is not null)
                    {
                        result.Warnings.Add("A non-blocking coordination poll is included for follow-up choices, but LocalGPT generated the requested sandbox artifact because no unresolved architecture gate remained.");
                    }

                    result.Artifacts.AddRange(await artifactService.CreateImplementationArtifactsAsync(request, result, cancellationToken).ConfigureAwait(false));
                }
                else if (request.GenerateImplementationArtifact && adviceOnlyPrompt)
                {
                    result.Warnings.Add("Implementation artifacts were not generated because this is an advice, review, release-readiness, or diagnostic prompt. Ask explicitly for a downloadable source artifact when files are wanted.");
                }
                else if (request.GenerateImplementationArtifact)
                {
                    result.Warnings.Add("Implementation artifacts were not generated because the user prompt did not explicitly ask LocalGPT to generate, create, or continue a downloadable/code artifact. This prevents normal advice, review, or release-readiness chats from producing unrelated zip files.");
                }

                result.KnowledgeEntryId = await knowledgeService.SaveFromCouncilRunAsync(result, cancellationToken).ConfigureAwait(false);

                result.CompletedAtUtc = DateTime.UtcNow;
                result.LogPath = await WriteLogAsync(result, cancellationToken, logger).ConfigureAwait(false);

                if (request.SaveToMemory)
                    result.MemoryConversationId = await SaveToMemoryAsync(request, result, continuedConversation, cancellationToken).ConfigureAwait(false);

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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RunAsync request {request.ToString()}");
                return null;
            }
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
            try
            {
                progressMessage?.Invoke($"Starting council phase: round {round}, {phase}, role {role}.");
                // A single append-only DXAIChat response cannot safely interleave nested
                // HTML from multiple model streams. Keep streamed presentation ordered;
                // non-streaming council runs still honor configured model parallelism.
                var effectiveMaxParallelModels = streamUpdate is null ? maxParallelModels : 1;
                using var gate = new SemaphoreSlim(effectiveMaxParallelModels, effectiveMaxParallelModels);
                var tasks = participants
                    .Select(async modelName =>
                    {
                        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
                        try
                        {
                            var participantGpuLayers = MultiModelCouncilServiceResolveParticipantOllamaNumGpu(modelName, ollamaNumGpu, logger);
                            progressMessage?.Invoke($"Starting {modelName}: {phase} / {role}. Ollama num_gpu={(participantGpuLayers?.ToString() ?? "auto")}.");
                            var step = await RunParticipantAsync(baseUri, modelName, participants, round, phase, role, promptFactory(modelName), bootstrap, maxOutputTokens, keepAlive, participantGpuLayers, maxContextTokens, modelTimeoutSeconds, streamUpdate, cancellationToken).ConfigureAwait(false);
                            ArgumentNullException.ThrowIfNull(step);
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
                    var completed = await Task.WhenAny(pending).ConfigureAwait(false);
                    pending.Remove(completed);
                    var step = await completed.ConfigureAwait(false);
                    ArgumentNullException.ThrowIfNull(step);
                    steps.Add(step);
                }

                var participantOrder = participants
                    .Select((modelName, index) => new { modelName, index })
                    .ToDictionary(item => item.modelName, item => item.index, StringComparer.OrdinalIgnoreCase);

                foreach (var step in steps.OrderBy(step => participantOrder.TryGetValue(step.ModelName, out var index) ? index : int.MaxValue))
                {
                    MultiModelCouncilServiceAddOrderedStep(result, step, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council phase failed for round {Round}, role {Role}, participant count {ParticipantCount}, " +
                    "max output {MaxOutputTokens}, max parallel {MaxParallelModels}, max context {MaxContextTokens}, timeout {TimeoutSeconds}s.",
                    round,
                    role,
                    participants.Count,
                    maxOutputTokens,
                    maxParallelModels,
                    maxContextTokens,
                    modelTimeoutSeconds);
            }
        }
        private List<string> SelectParticipants(MultiModelCouncilRequest request)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SelectParticipants request {request.ToString()}");
                return new();
            }
            
        }

        private async Task<MultiModelCouncilStep?> RunParticipantAsync(
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
            try
            {
                var started = DateTime.UtcNow;
                var stopwatch = Stopwatch.StartNew();

                try
                {
                    using var client = new OllamaThinkingChatClient(new OllamaCoreOptions
                    {
                        Uri = baseUri,
                        ModelName = modelName
                    },
                    logger,
                    keepAlive,
                    maxContextTokens,
                    TimeSpan.FromSeconds(modelTimeoutSeconds + 15),
                    ollamaNumGpu,
                    formatterFactory,
                    protocolResolver,
                    promptConfigService);


                    using var participantCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    participantCts.CancelAfter(TimeSpan.FromSeconds(modelTimeoutSeconds));

                    var messages = new List<ChatMessage>();
                    if (!string.IsNullOrWhiteSpace(bootstrap))
                        messages.Add(new ChatMessage(ChatRole.System, bootstrap));
                    messages.Add(new ChatMessage(ChatRole.System, CouncilChatStringFunctions.MultiModelCouncilServiceCreateCouncilSystemPrompt(modelName, councilMembers,logger)));
                    messages.Add(new ChatMessage(ChatRole.User, prompt));

                    var streamId = Guid.NewGuid().ToString("N");
                    streamUpdate?.Invoke($"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} / {role} live output")}</summary>\n\n");
                    var builder = new StringBuilder();
                    ArgumentNullException.ThrowIfNull(client);
                    ArgumentNullException.ThrowIfNull(messages);
                    await foreach (var update in client.GetStreamingResponseAsync(
                        (List<ChatMessage>) messages,
                        new ChatOptions
                        {
                            MaxOutputTokens = Math.Clamp(maxOutputTokens, MinOutputTokens, MaxOutputTokens),
                            Temperature = 0.2f
                        },
                        participantCts.Token).WithCancellation(participantCts.Token).ConfigureAwait(true))
                    {
                        builder.Append(update.Text);
                        streamUpdate?.Invoke(update.Text);
                    }

                    streamUpdate?.Invoke($"\n\n</details><!--localgpt-council-stream-complete:{streamId}-->\n\n");

                    var content = builder.ToString();
                    var thinking = CouncilChatStringFunctions.MultiModelCouncilServiceExtractThinking(content,logger);
                    var visibleContent = CouncilChatStringFunctions.MultiModelCouncilServiceStripThinking(content, logger);
                    if (string.IsNullOrWhiteSpace(visibleContent) && !string.IsNullOrWhiteSpace(thinking))
                        visibleContent = $"_{modelName} returned thinking during {phase}, but no final visible answer. Increase max output tokens or ask for a shorter final answer._";

                    if (MultiModelCouncilServiceIsThinkingOnlyCouncilContent(visibleContent, logger))
                    {
                        var recovery = await MultiModelCouncilServiceRunFinalOnlyRecoveryAsync(
                            client,
                            modelName,
                            phase,
                            messages,
                            Math.Clamp(Math.Min(Math.Max(maxOutputTokens, 2048), 8192), MinOutputTokens, MaxOutputTokens),
                            streamUpdate,
                            participantCts.Token,logger).ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(recovery.Content))
                            content = $"{content}{Environment.NewLine}{Environment.NewLine}{recovery.Content}";
                        if (!string.IsNullOrWhiteSpace(recovery.Thinking))
                            thinking = string.Join(Environment.NewLine, new[] { thinking, recovery.Thinking }.Where(text => !string.IsNullOrWhiteSpace(text)));
                        if (MultiModelCouncilServiceIsSubstantiveCouncilContent(recovery.VisibleContent, logger))
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
                    if (MultiModelCouncilServiceShouldUnloadAfterParticipant(keepAlive, logger))
                        await RequestOllamaUnloadAsync(baseUri, modelName, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council participant failed for model {ModelName}, round {Round}, phase {Phase}, role {Role}, max output {MaxOutputTokens}, max context {MaxContextTokens}, timeout {TimeoutSeconds}s.", modelName, round, phase, role, maxOutputTokens, maxContextTokens, modelTimeoutSeconds);
                return null;
            }
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
                    unloadCts.Token).ConfigureAwait(false);

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
            try
            {
                var options = optionsRoot.CurrentValue.AICore ?? new AICoreOptions();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(options.OllamaCore.Uri))
                {
                    seen.Add($"{CouncilChatStringFunctions.MultiModelCouncilServiceNormalizeEndpoint(options.OllamaCore.Uri, logger)}|{options.OllamaCore.ModelName}");
                    yield return options.OllamaCore;
                }

                foreach (var provider in options.OllamaCores.Where(provider => !string.IsNullOrWhiteSpace(provider.Uri)))
                {
                    if (seen.Add($"{CouncilChatStringFunctions.MultiModelCouncilServiceNormalizeEndpoint(provider.Uri, logger)}|{provider.ModelName}"))
                        yield return provider;
                }
            }
            finally
            {
                logger.LogInformation($"Ended GetConfiguredOllamaProviders");
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

                var tags = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/tags", cancellationToken).ConfigureAwait(false) ?? new OllamaTagsResponse();
                var running = await MultiModelCouncilServiceProbeRunningModelNamesAsync(http, cancellationToken, logger).ConfigureAwait(false);

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
        public static async Task<(string Content, string VisibleContent, string? Thinking)> MultiModelCouncilServiceRunFinalOnlyRecoveryAsync(
            IChatClient client,
            string modelName,
            string phase,
            IReadOnlyList<ChatMessage> originalMessages,
            int maxOutputTokens,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var messages = originalMessages.ToList();
                messages.Add(new ChatMessage(ChatRole.User, $"""
                Your previous {phase} response for LocalGPT produced model thinking/status but no user-visible final answer.
                Do not analyze again. Do not emit hidden reasoning. Do not use tool calls.
                Emit only the final visible answer now in concise Markdown bullets.
                Start with: Final answer:
                """));

                var builder = new StringBuilder();
                var streamId = Guid.NewGuid().ToString("N");
                streamUpdate?.Invoke($"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} final-answer recovery")}</summary>\n\n");
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

                streamUpdate?.Invoke($"\n\n</details><!--localgpt-council-stream-complete:{streamId}-->\n\n");
                var content = builder.ToString();
                var thinking = CouncilChatStringFunctions.MultiModelCouncilServiceExtractThinking(content, logger);
                var visibleContent = CouncilChatStringFunctions.MultiModelCouncilServiceStripThinking(content, logger);
                return (content, visibleContent, thinking);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council final-only recovery failed for model {ModelName}, phase {Phase}, message count {MessageCount}, max output {MaxOutputTokens}.", modelName, phase, originalMessages.Count, maxOutputTokens);
                return (string.Empty, string.Empty, null);
            }

        }

        public static void MultiModelCouncilServiceAddOrderedStep(MultiModelCouncilResult result, MultiModelCouncilStep step, ILogger logger)
        {
            try
            {
                step.SortOrder = result.Steps.Count;
                if (step.CouncilMembers.Count == 0)
                    step.CouncilMembers = result.ModelNames.ToList();
                result.Steps.Add(step);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddOrderedStep {ex.ToString()} result {result.ToString()} step {step.ToString()}");
            }
        }

        public static string MultiModelCouncilServiceSelectConsensusContent(MultiModelCouncilResult result, MultiModelCouncilStep consensusStep , ILogger logger)
        {
            try
            {
                var consensus = consensusStep.VisibleContent.Trim();
                if (MultiModelCouncilServiceIsSubstantiveCouncilContent(consensus, logger))
                    return consensus;

                result.Warnings.Add($"{consensusStep.ModelName} returned a non-substantive consensus during {consensusStep.Phase}; LocalGPT used the latest substantive council step as the final-answer fallback.");

                var fallback = result.Steps
                    .Where(step => !ReferenceEquals(step, consensusStep))
                    .OrderByDescending(step => step.SortOrder)
                    .Select(step => step.VisibleContent.Trim())
                    .FirstOrDefault(filter => MultiModelCouncilServiceIsSubstantiveCouncilContent(filter,logger));

                if (!string.IsNullOrWhiteSpace(fallback))
                    return fallback;

                return $"_{consensusStep.ModelName} did not return a substantive consensus answer. Retry with a higher output token budget, a smaller model set, or a shorter prompt._";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SelectConsensusContent {ex.ToString()} result {result.ToString()} consensusStep {consensusStep.ToString()}");
                return string.Empty;
            }
        }

        public static bool MultiModelCouncilServiceIsSubstantiveCouncilContent(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                var trimmed = content.Trim();
                if (trimmed.Length < 80)
                    return false;

                var letterCount = trimmed.Count(char.IsLetter);
                var wordCount = GlobalVariableSlopCollectionToRemove.WordPattern().Matches(trimmed).Count;
                return letterCount >= 40 && wordCount >= 10;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSubstantiveCouncilContent {ex.ToString()} content {content.ToString()}");
                return false;
            }
        }

        public static bool MultiModelCouncilServiceIsThinkingOnlyCouncilContent(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                return content.Contains("No final answer was emitted", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("returned thinking during", StringComparison.OrdinalIgnoreCase) ||
                    content.Contains("no final visible answer", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsThinkingOnlyCouncilContent {ex.ToString()} content {content.ToString()}");
                return false;
            }
        }
        public static string MultiModelCouncilServiceGetCouncilKeepAlive(MultiModelCouncilRequest request, int participantCount, int maxParallelModels, ILogger logger)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.OllamaKeepAlive))
                    return request.OllamaKeepAlive.Trim();

                return participantCount > 1 && maxParallelModels == 1
                    ? "0s"
                    : "3m";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetCouncilKeepAlive request {request.ToString()} participantCount {participantCount.ToString()} maxParallelModels {maxParallelModels.ToString()}");
                return string.Empty;
            }
        }

        public static bool MultiModelCouncilServiceShouldUnloadAfterParticipant(string keepAlive, ILogger logger)
        {
            try
            {
                var normalized = keepAlive.Trim();
                return normalized.Equals("0", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("0s", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("0m", StringComparison.OrdinalIgnoreCase) ||
                    normalized.Equals("0h", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ShouldUnloadAfterParticipant keepAlive {keepAlive.ToString()}");
                return false;
            }
        }

        public static int? MultiModelCouncilServiceResolveParticipantOllamaNumGpu(string modelName, int? requestedNumGpu, ILogger logger)
        {
            try
            {
                if (requestedNumGpu is not null)
                    return requestedNumGpu;

                return MultiModelCouncilServiceIsHeavyGpuRiskModel(modelName, logger) ? DefaultHeavyModelGpuLayers : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ResolveParticipantOllamaNumGpu modelName {modelName.ToString()} requestedNumGpu {requestedNumGpu.ToString()}");
                return null;
            }

        }

        public static bool MultiModelCouncilServiceIsHeavyGpuRiskModel(string modelName, ILogger logger)
        {
            try
            {
                return modelName.Contains("qwen", StringComparison.OrdinalIgnoreCase) ||
    modelName.Contains("gwen", StringComparison.OrdinalIgnoreCase) ||
    modelName.Contains("gemma", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ResolveParticipantOllamaNumGpu modelName {modelName.ToString()}");
                return false;
            }
        }


        public static async Task<HashSet<string>> MultiModelCouncilServiceProbeRunningModelNamesAsync(HttpClient http, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var running = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/ps", cancellationToken).ConfigureAwait(false) ?? new OllamaTagsResponse();
                return running.Models.Select(model => model.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ProbeRunningModelNamesAsync http {http.ToString()}.");
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public async Task<ChatMemoryConversationSnapshot?> LoadContinuationConversationAsync(Guid? conversationId, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                if (conversationId is not Guid id)
                    return null;

                try
                {
                    return await chatMemory.LoadConversationAsync(id, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error in LoadContinuationConversationAsync Could not load council continuation conversation {ConversationId}.", id);
                    return null;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ProbeRunningModelNamesAsync conversationId {conversationId.ToString()}.");
                return null;
            }
            
        }

        public static string MultiModelCouncilServiceBuildContinuationContext(ChatMemoryConversationSnapshot? conversation, ILogger logger)
        {
            try
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
                        .AppendLine(CouncilChatStringFunctions.MultiModelCouncilServiceTrimCouncilText(CouncilChatStringFunctions.MultiModelCouncilServiceStripThinking(message.Content, logger), 700, logger));
                }

                builder.AppendLine("Every council member must treat this as selected continuation context. Preserve user decisions from prior polls unless the user explicitly changes them.");
                return builder.ToString().Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildContinuationContext {conversation?.ToString()}");
                return string.Empty;
            }
        }

        public static string MultiModelCouncilServiceAppendPromptSection(string existing, string title, string content, ILogger logger)
        {
            try
            {
                var section = $"{title}:{Environment.NewLine}{content}".Trim();
                return string.IsNullOrWhiteSpace(existing)
                    ? section
                    : $"{existing.Trim()}{Environment.NewLine}{Environment.NewLine}{section}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not append council prompt section {SectionTitle}.", title);
                return string.Empty;
            }
        }

        public async Task<Guid?> SaveToMemoryAsync(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            ChatMemoryConversationSnapshot? continuedConversation,
            CancellationToken cancellationToken)
        {
            try
            {
                var messages = continuedConversation is null
    ? new List<BlazorChatMessage>()
    : continuedConversation.Messages
        .Where(message => !message.Typing && !string.IsNullOrWhiteSpace(message.Content))
        .ToList();

                messages.Add(new BlazorChatMessage(
                    ChatRole.User,
                    MultiModelCouncilServiceBuildCouncilRequestMemoryMessage(request, result, continuedConversation is not null, logger),
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
                        MultiModelCouncilServiceBuildMemoryMessage(step, logger),
                        new List<AIChatUploadFileInfo>()));
                }

                if (result.UserPoll is not null)
                {
                    messages.Add(new BlazorChatMessage(
                        ChatRole.Assistant,
                        CouncilChatStringFunctions.MultiModelCouncilServiceBuildPollMarkdown(result.UserPoll, logger),
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

                if (result.Artifacts.Count > 0)
                {
                    messages.Add(new BlazorChatMessage(
                        ChatRole.Assistant,
                        CouncilChatStringFunctions.MultiModelCouncilServiceBuildArtifactsMarkdown(result.Artifacts, logger),
                        new List<AIChatUploadFileInfo>()));
                }

                return await chatMemory.SaveConversationAsync(
                    $"AI Council - {string.Join(" + ", result.ModelNames)}",
                    messages,
                    continuedConversation?.Id,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SaveToMemoryAsync request {request?.ToString()} result {result?.ToString()} continuedConversation {continuedConversation?.ToString()}");
                return null;
            }

        }

        public static string MultiModelCouncilServiceBuildCouncilRequestMemoryMessage(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result,
            bool isContinuation, ILogger logger)
        {
            try
            {
                var label = isContinuation ? "AI Council continuation request" : "AI Council request";
                return $"""
                {label}:
                Council members: {string.Join(", ", result.ModelNames)}

                {request.Prompt}
                """.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCouncilRequestMemoryMessage request {request?.ToString()} result {result?.ToString()} isContinuation {isContinuation.ToString()}");
                return string.Empty;
            }
        }

        public static string MultiModelCouncilServiceBuildMemoryMessage(MultiModelCouncilStep step, ILogger logger)
        {
            try
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
                        .AppendLine("<details class=\"model-thinking\">")
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildMemoryMessage step {step?.ToString()}");
                return string.Empty;
            }
            
        }

        public async Task<string> WriteLogAsync(MultiModelCouncilResult result, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var directory = Path.Combine(
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "LocalGPT",
                     "CouncilLogs");
                Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, $"council-{DateTime.Now:yyyyMMdd-HHmmss}-{result.RunId:N}.md");
                await System.IO.File.WriteAllTextAsync(path, CouncilChatStaticsGeneral.MultiModelCouncilServiceBuildLogMarkdown(result, logger), cancellationToken).ConfigureAwait(false);
                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteLogAsync result {result?.ToString()}");
                return string.Empty;
            }
        }

  


    }
}
