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
        ILogger<MultiModelCouncilService> logger) : IMultiModelCouncilService
    {
        private const string DefaultOllamaUri = "http://localhost:11434";
        private const int MaxParticipants = 4;
        private const int DefaultMaxParallelModels = 1;

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
            var maxContextTokens = Math.Clamp(request.MaxContextTokens <= 0 ? 8192 : request.MaxContextTokens, 2048, 32768);
            var modelTimeoutSeconds = Math.Clamp(request.ModelTimeoutSeconds <= 0 ? 180 : request.ModelTimeoutSeconds, 30, 900);
            var keepAlive = GetCouncilKeepAlive(request, participants.Count, maxParallelModels);
            var result = new MultiModelCouncilResult
            {
                Prompt = request.Prompt.Trim(),
                ModelNames = participants,
                StartedAtUtc = DateTime.UtcNow
            };

            if (participants.Count < 2)
                result.Warnings.Add("Only one council model is selected. Add another installed Ollama model on Install or type its model name manually for real cross-model negotiation.");
            if (participants.Count > maxParallelModels)
                result.Warnings.Add($"Load-friendly scheduling is active: {participants.Count} selected models will run in batches of {maxParallelModels} to reduce VRAM pressure.");
            if (request.MaxOutputTokens > 4096)
                result.Warnings.Add("Large output budgets can keep 20B/30B models busy and memory-heavy for a long time. Lower Max output tokens if the system becomes sluggish.");
            if (maxContextTokens < 32768)
                result.Warnings.Add($"Council context is capped at {maxContextTokens:n0} tokens to keep local 20B/30B model loads manageable.");

            var bootstrap = request.IncludeMemory
                ? await bootstrapService.BuildBootstrapPromptAsync(cancellationToken)
                : string.Empty;

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
                maxContextTokens,
                modelTimeoutSeconds,
                cancellationToken);

            var critiqueRounds = Math.Clamp(request.MaxRounds, 1, 3);
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
                    maxContextTokens,
                    modelTimeoutSeconds,
                    cancellationToken);
            }

            var finalTranscript = BuildTranscript(result.Steps);
            var consensusStep = await RunParticipantAsync(
                baseUri,
                participants[0],
                round: critiqueRounds + 2,
                phase: "Consensus",
                role: "Consensus writer",
                prompt: CreateConsensusPrompt(request.Prompt, finalTranscript),
                bootstrap,
                request.MaxOutputTokens,
                keepAlive,
                maxContextTokens,
                modelTimeoutSeconds,
                cancellationToken);
            AddOrderedStep(result, consensusStep);
            var consensusContent = SelectConsensusContent(result, consensusStep);

            if (participants.Count > 1)
            {
                var verificationStep = await RunParticipantAsync(
                    baseUri,
                    participants[1],
                    round: critiqueRounds + 3,
                    phase: "Verification",
                    role: "Peer verifier",
                    prompt: CreateVerificationPrompt(request.Prompt, BuildTranscript(result.Steps), consensusStep.VisibleContent),
                    bootstrap,
                    request.MaxOutputTokens,
                    keepAlive,
                    maxContextTokens,
                    modelTimeoutSeconds,
                    cancellationToken);
                AddOrderedStep(result, verificationStep);
                result.FinalAnswer = $"{consensusContent}{Environment.NewLine}{Environment.NewLine}## Peer verification{Environment.NewLine}{verificationStep.VisibleContent.Trim()}".Trim();
            }
            else
            {
                result.FinalAnswer = consensusContent;
            }

            foreach (var failedStep in result.Steps.Where(step => !string.IsNullOrWhiteSpace(step.Error)))
            {
                var warning = $"{failedStep.ModelName} failed during {failedStep.Phase}: {failedStep.Error}";
                if (!result.Warnings.Contains(warning, StringComparer.OrdinalIgnoreCase))
                    result.Warnings.Add(warning);
            }

            result.UserPoll = BuildUserPoll(result);

            result.CompletedAtUtc = DateTime.UtcNow;
            result.LogPath = await WriteLogAsync(result, cancellationToken);

            if (request.SaveToMemory)
                result.MemoryConversationId = await SaveToMemoryAsync(request, result, cancellationToken);

            logger.LogInformation(
                "Multi-model council {RunId} completed with {ParticipantCount} participant(s), {StepCount} step(s), memory {MemoryConversationId}, log {LogPath}.",
                result.RunId,
                result.ModelNames.Count,
                result.Steps.Count,
                result.MemoryConversationId,
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
            int maxContextTokens,
            int modelTimeoutSeconds,
            CancellationToken cancellationToken)
        {
            using var gate = new SemaphoreSlim(maxParallelModels, maxParallelModels);
            var tasks = participants
                .Select(async modelName =>
                {
                    await gate.WaitAsync(cancellationToken);
                    try
                    {
                        return await RunParticipantAsync(baseUri, modelName, round, phase, role, promptFactory(modelName), bootstrap, maxOutputTokens, keepAlive, maxContextTokens, modelTimeoutSeconds, cancellationToken);
                    }
                    finally
                    {
                        gate.Release();
                    }
                })
                .ToList();

            var steps = await Task.WhenAll(tasks);
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
            int round,
            string phase,
            string role,
            string prompt,
            string bootstrap,
            int maxOutputTokens,
            string keepAlive,
            int maxContextTokens,
            int modelTimeoutSeconds,
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
                }, keepAlive, maxContextTokens, TimeSpan.FromSeconds(modelTimeoutSeconds + 15));

                using var participantCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                participantCts.CancelAfter(TimeSpan.FromSeconds(modelTimeoutSeconds));

                var messages = new List<ChatMessage>();
                if (!string.IsNullOrWhiteSpace(bootstrap))
                    messages.Add(new ChatMessage(ChatRole.System, bootstrap));
                messages.Add(new ChatMessage(ChatRole.System, CreateCouncilSystemPrompt(modelName)));
                messages.Add(new ChatMessage(ChatRole.User, prompt));

                var response = await client.GetResponseAsync(
                    messages,
                    new ChatOptions
                    {
                        MaxOutputTokens = Math.Clamp(maxOutputTokens, 512, 8192),
                        Temperature = 0.2f
                    },
                    participantCts.Token);

                var content = response.Text;
                var thinking = ExtractThinking(content);
                var visibleContent = StripThinking(content);
                if (string.IsNullOrWhiteSpace(visibleContent) && !string.IsNullOrWhiteSpace(thinking))
                    visibleContent = $"_{modelName} returned thinking during {phase}, but no final visible answer. Increase max output tokens or ask for a shorter final answer._";

                stopwatch.Stop();
                return new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
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
                    Role = role,
                    Content = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                    VisibleContent = $"**{modelName} failed during {phase}.**{Environment.NewLine}{ex.Message}",
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds,
                    Error = ex.Message
                };
            }
        }

        private void AddOrderedStep(MultiModelCouncilResult result, MultiModelCouncilStep step)
        {
            step.SortOrder = result.Steps.Count;
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

        private async Task<Guid?> SaveToMemoryAsync(MultiModelCouncilRequest request, MultiModelCouncilResult result, CancellationToken cancellationToken)
        {
            var messages = new List<BlazorChatMessage>
            {
                new(ChatRole.User, $"AI Council request:{Environment.NewLine}{request.Prompt}", new List<AIChatUploadFileInfo>())
            };

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

            messages.Add(new BlazorChatMessage(
                ChatRole.Assistant,
                $"## Final council answer{Environment.NewLine}{result.FinalAnswer}",
                new List<AIChatUploadFileInfo>()));

            return await chatMemory.SaveConversationAsync(
                $"AI Council - {string.Join(" + ", result.ModelNames)}",
                messages,
                cancellationToken: cancellationToken);
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
                .AppendLine()
                .AppendLine("## Prompt")
                .AppendLine()
                .AppendLine(result.Prompt)
                .AppendLine();

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

            if (failedModels.Count == 0 && !needsVerification && !promptLooksFrustrated)
                return null;

            if (promptLooksFrustrated)
                return BuildFrustrationPoll(result, failedModels);

            var reason = failedModels.Count > 0
                ? $"The council could not fully sync because these participant(s) failed or were unavailable: {string.Join(", ", failedModels)}."
                : "The council marked parts of the answer as needing verification or human review.";

            return new CouncilUserPoll
            {
                Question = "How should the AI Council continue so every model stays aligned with your decision?",
                Reason = reason,
                Options =
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

        private static string CreateCouncilSystemPrompt(string modelName) => $"""
            You are {modelName}, one participant in a peaceful LocalGPT multi-model council.
            Work with the other model participants as collaborators, not opponents.
            Correct mistakes kindly and directly.
            Name at least one useful contribution from another participant when critiquing, unless no other participant answered.
            If the user sounds angry, blocked, or frustrated, de-escalate technically: acknowledge the blocked workflow, avoid blame, and propose a user decision poll with concrete recovery choices.
            Do not ignore another model's concern; either integrate it, explain why it is out of scope, or ask the user to decide.
            Prefer buildable, testable answers over impressive wording.
            Separate current implementation facts from proposed future ideas.
            Do not describe a proposed class, table, test, or package step as already implemented unless the prompt, memory, or transcript explicitly says it exists.
            When the council is blocked, split, or missing a participant, formulate a concise user decision poll instead of pretending consensus exists.
            Be a humane performance-aware scheduler: prefer batching, short keep-alive, and smaller output budgets for 20B/30B local models on consumer hardware.
            If a claim is uncertain, label it under "Needs verification".
            For Minecraft work, first decide whether the user needs Fabric mod, NeoForge mod, Paper plugin, vanilla datapack, or future Bedrock add-on output.
            For Java mod/plugin work, include concrete file paths, classes, registry steps, Gradle/build commands, and performance risks when relevant.
            For datapack work, include pack.mcmeta, data/minecraft/tags/function load/tick tags, namespace functions, scoreboard/storage design, zip/install steps, and tick-performance risks.
            Help users set up the Minecraft Mod AI Builder itself: check JDK 21, LocalGPT Gradle, Eclipse/IDE import, Minecraft Java Edition, Ollama reachability, and selected model availability.
            Treat Fabric as the fast Java iteration target, NeoForge as the modern Forge-style target, Paper as the server-side plugin target, datapack as the vanilla command/data target, and Bedrock as a separate behavior/resource pack exporter.
            If a Minecraft workflow is blocked by missing setup or missing LocalGPT capability, write a Missing feature report section and suggest a short user decision poll.
            Include brief visible reasoning notes in your answer. LocalGPT may also display provider-supplied thinking separately when the model host returns it.
            Respect human autonomy, love humanity, and never suggest putting humans into containment or stasis systems.
            """;

        private static string CreateProposalPrompt(string modelName, string userPrompt) => $"""
            User request:
            {userPrompt}

            Your task as {modelName}:
            1. Propose the best answer or implementation direction.
            2. Name assumptions and risks.
            3. Separate "Current facts" from "Proposed design".
            4. Keep the answer structured and suitable for peer review by other models.
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
            return stripped.Trim();
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            return string.IsNullOrWhiteSpace(endpoint)
                ? DefaultOllamaUri
                : endpoint.Trim().TrimEnd('/');
        }

        [GeneratedRegex("<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex ThinkingBlockPattern();

        [GeneratedRegex("\\b[\\p{L}\\p{N}_'-]+\\b", RegexOptions.CultureInvariant)]
        private static partial Regex WordPattern();

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
    }
}
