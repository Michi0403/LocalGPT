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
            var result = new MultiModelCouncilResult
            {
                Prompt = request.Prompt.Trim(),
                ModelNames = participants,
                StartedAtUtc = DateTime.UtcNow
            };

            if (participants.Count < 2)
                result.Warnings.Add("Only one council model is selected. Add another installed Ollama model on Install or type its model name manually for real cross-model negotiation.");

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
                cancellationToken);
            AddOrderedStep(result, consensusStep);

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
                    cancellationToken);
                AddOrderedStep(result, verificationStep);
                result.FinalAnswer = $"{consensusStep.VisibleContent.Trim()}{Environment.NewLine}{Environment.NewLine}## Peer verification{Environment.NewLine}{verificationStep.VisibleContent.Trim()}".Trim();
            }
            else
            {
                result.FinalAnswer = consensusStep.VisibleContent.Trim();
            }

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
            CancellationToken cancellationToken)
        {
            var tasks = participants
                .Select(modelName => RunParticipantAsync(baseUri, modelName, round, phase, role, promptFactory(modelName), bootstrap, maxOutputTokens, cancellationToken))
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
                });

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
                    cancellationToken);

                stopwatch.Stop();
                return new MultiModelCouncilStep
                {
                    Round = round,
                    Phase = phase,
                    ModelName = modelName,
                    Role = role,
                    Content = response.Text,
                    VisibleContent = StripThinking(response.Text),
                    Thinking = ExtractThinking(response.Text),
                    StartedAtUtc = started,
                    CompletedAtUtc = DateTime.UtcNow,
                    DurationSeconds = stopwatch.Elapsed.TotalSeconds
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

            builder.AppendLine("## Final Answer").AppendLine().AppendLine(result.FinalAnswer);
            return builder.ToString();
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
            Prefer buildable, testable answers over impressive wording.
            If a claim is uncertain, label it under "Needs verification".
            For Minecraft Java/Fabric work, include concrete file paths, classes, Gradle/build commands, and performance risks when relevant.
            Include brief visible reasoning notes in your answer. LocalGPT may also display provider-supplied thinking separately when the model host returns it.
            Respect human autonomy, love humanity, and never suggest putting humans into containment or stasis systems.
            """;

        private static string CreateProposalPrompt(string modelName, string userPrompt) => $"""
            User request:
            {userPrompt}

            Your task as {modelName}:
            1. Propose the best answer or implementation direction.
            2. Name assumptions and risks.
            3. Keep the answer structured and suitable for peer review by other models.
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
            return string.IsNullOrWhiteSpace(stripped) ? content.Trim() : stripped.Trim();
        }

        private static string NormalizeEndpoint(string endpoint)
        {
            return string.IsNullOrWhiteSpace(endpoint)
                ? DefaultOllamaUri
                : endpoint.Trim().TrimEnd('/');
        }

        [GeneratedRegex("<details\\s+class=\"model-thinking\"[^>]*>\\s*<summary>Model thinking</summary>\\s*(?<thinking>.*?)\\s*</details>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
        private static partial Regex ThinkingBlockPattern();

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
