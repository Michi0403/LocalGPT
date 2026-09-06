using DevExpress.AIIntegration.Blazor.Chat;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Channels;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates multi model council behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class MultiModelCouncilService
    {
    /// <summary>Runs one bounded same-member retry when a model generically refuses a solvable assigned role task.</summary>
        /// <summary>
        /// Performs multi model council service run role compliance recovery as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="client">The already-configured provider client for the same member.</param>
        /// <param name="modelName">Provider-qualified member identity.</param>
        /// <param name="phase">Current workflow phase.</param>
        /// <param name="role">Current assigned role.</param>
        /// <param name="originalMessages">Original authoritative role-task messages.</param>
        /// <param name="maxOutputTokens">Bounded retry output limit.</param>
        /// <param name="streamUpdate">Optional live transcript sink.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns>The corrective retry content, visible answer and provider thinking.</returns>
        private async Task<(string Content, string VisibleContent, string? Thinking)> MultiModelCouncilServiceRunRoleComplianceRecoveryAsync(
            IChatClient client,
            string modelName,
            string phase,
            string role,
            IReadOnlyList<ChatMessage> originalMessages,
            int maxOutputTokens,
            Action<string>? streamUpdate,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            try
            {
                var messages = originalMessages.ToList();
                messages.Add(new ChatMessage(ChatRole.User, $"""
                CORRECTIVE ROLE EXECUTION — this is your one bounded retry.
                Your assigned Council role is: {role}.
                Your current workflow phase is: {phase}.
                The role task and all required input were already supplied above. The original user request is background context, not a replacement task.
                Do not decline merely because you are an AI/model, do not ask for the task again, do not delegate your role, and do not redesign the Council workflow.
                Execute the assigned role task now with the information and capabilities actually available to you. If some detail is uncertain, make the best bounded attempt and state that uncertainty inside the result.
                Return only the substantive result required by this role.
                """));

                var builder = new StringBuilder();
                var repetitionWatchdog = new ProviderStreamRepetitionWatchdog(catalog, logger);
                using var recoveryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var streamId = Guid.NewGuid().ToString("N");
                var streamPanelOpened = streamUpdate is not null;
                streamUpdate?.Invoke($"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} / {role} corrective role retry")}</summary>\n\n");
                try
                {
                    await foreach (var update in client.GetStreamingResponseAsync(
                        messages,
                        new ChatOptions { MaxOutputTokens = maxOutputTokens, Temperature = 0.1f },
                        recoveryCts.Token).WithCancellation(recoveryCts.Token).ConfigureAwait(false))
                    {
                        builder.Append(update.Text);
                        streamUpdate?.Invoke(update.Text);
                        if (!councilRuntime.IsLocalGptStreamingStatusUpdate(update.Text, logger))
                        {
                            var repetitionFailure = repetitionWatchdog.Observe(update.Text);
                            if (repetitionFailure is not null)
                            {
                                streamUpdate?.Invoke(
                                    $"\n\n> **LocalGPT repetition watchdog:** the corrective role retry entered sustained repeated generation and was stopped. {WebUtility.HtmlEncode(repetitionFailure.Message)}\n\n");
                                recoveryCts.Cancel();
                                throw repetitionFailure;
                            }
                        }
                    }
                }
                finally
                {
                    if (streamPanelOpened)
                        streamUpdate?.Invoke($"\n\n</details><!--localgpt-council-stream-complete:{streamId}-->\n\n");
                }

                var content = builder.ToString();
                return (
                    content,
                    councilText.MultiModelCouncilServiceStripThinking(content, logger),
                    councilText.MultiModelCouncilServiceExtractThinking(content, logger));
            }
            catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(exception, "Council role-compliance retry was cancelled for {ModelName} in {Phase}.", modelName, phase);
                return (string.Empty, string.Empty, null);
            }
            catch (ProviderStreamRepetitionException exception)
            {
                logger.LogWarning(
                    "Council repetition watchdog stopped role-compliance retry for {ModelName} in {Phase}: period {PeriodTokens} tokens, agreement {Agreement:P1}, observed {ObservedSeconds:0.0}s.",
                    modelName,
                    phase,
                    exception.PeriodTokens,
                    exception.Agreement,
                    exception.ObservedSeconds);
                return (string.Empty, string.Empty, null);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Council role-compliance retry failed for {ModelName} in {Phase}; prompt and response content were omitted.", modelName, phase);
                return (string.Empty, string.Empty, null);
            }
        }

        /// <summary>Detects narrow generic non-performance/refusal text without treating safety refusals as role failures.</summary>
        /// <param name="content">Visible provider result.</param>
        /// <param name="logger">Diagnostics logger.</param>
        /// <returns><see langword="true"/> only for generic capability/refusal or already-supplied-task requests.</returns>
        private bool MultiModelCouncilServiceLooksLikeGenericRoleRefusal(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content) || content.Length > 1800)
                    return false;
                var lower = content.Trim().ToLowerInvariant();
                if (lower.Contains("safety", StringComparison.Ordinal) || lower.Contains("harmful", StringComparison.Ordinal) || lower.Contains("illegal", StringComparison.Ordinal))
                    return false;
                return (lower.Contains("as an ai", StringComparison.Ordinal) &&
                        (lower.Contains("cannot", StringComparison.Ordinal) || lower.Contains("can't", StringComparison.Ordinal) || lower.Contains("do not have", StringComparison.Ordinal))) ||
                       lower.Contains("don't have the capability", StringComparison.Ordinal) ||
                       lower.Contains("do not have the capability", StringComparison.Ordinal) ||
                       lower.Contains("cannot execute tasks", StringComparison.Ordinal) ||
                       lower.Contains("cannot participate", StringComparison.Ordinal) ||
                       lower.Contains("please provide the task", StringComparison.Ordinal) ||
                       lower.Contains("please provide instructions", StringComparison.Ordinal);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Detecting Council role-refusal text failed; response content was omitted.");
                return false;
            }
        }

        /// <summary>
        /// Performs multi model council service run final only recovery as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="client">Chat client dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="phase">Phase value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="originalMessages">Chat message dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="maxOutputTokens">Max output tokens value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="streamUpdate">Stream update value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string content string visible content string thinking produced by the operation.</returns>
        public async Task<(string Content, string VisibleContent, string? Thinking)> MultiModelCouncilServiceRunFinalOnlyRecoveryAsync(
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
                Your previous {phase} response for LocalGPT produced provider thinking/status but no substantive user-visible final answer.
                Preserve normal provider-supplied thinking/self-correction if your runtime emits it, and use an exact registered DXFunction only when genuinely needed. LocalGPT keeps provider thinking and tool activity visibly separated from the final answer.
                Focus on finishing the task rather than restarting the analysis from scratch. You must emit a substantive final visible answer now in concise Markdown.
                Start the visible answer with: Final answer:
                """));

                var builder = new StringBuilder();
                var repetitionWatchdog = new ProviderStreamRepetitionWatchdog(catalog, logger);
                using var recoveryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var streamId = Guid.NewGuid().ToString("N");
                var streamPanelOpened = streamUpdate is not null;
                streamUpdate?.Invoke($"<details class=\"council-step council-live\" data-localgpt-stream-id=\"{streamId}\" open><summary>{WebUtility.HtmlEncode($"{modelName} — {phase} final-answer recovery")}</summary>\n\n");
                try
                {
                    await foreach (var update in client.GetStreamingResponseAsync(
                        messages,
                        new ChatOptions
                        {
                            MaxOutputTokens = maxOutputTokens,
                            Temperature = 0.1f
                        },
                        recoveryCts.Token).WithCancellation(recoveryCts.Token).ConfigureAwait(false))
                    {
                        builder.Append(update.Text);
                        streamUpdate?.Invoke(update.Text);
                        if (!councilRuntime.IsLocalGptStreamingStatusUpdate(update.Text, logger))
                        {
                            var repetitionFailure = repetitionWatchdog.Observe(update.Text);
                            if (repetitionFailure is not null)
                            {
                                streamUpdate?.Invoke(
                                    $"\n\n> **LocalGPT repetition watchdog:** the final-answer recovery entered sustained repeated generation and was stopped. {WebUtility.HtmlEncode(repetitionFailure.Message)}\n\n");
                                recoveryCts.Cancel();
                                throw repetitionFailure;
                            }
                        }
                    }
                }
                finally
                {
                    if (streamPanelOpened)
                        streamUpdate?.Invoke($"\n\n</details><!--localgpt-council-stream-complete:{streamId}-->\n\n");
                }
                var content = builder.ToString();
                var thinking = councilText.MultiModelCouncilServiceExtractThinking(content, logger);
                var visibleContent = councilText.MultiModelCouncilServiceStripThinking(content, logger);
                return (content, visibleContent, thinking);
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                logger.LogDebug(ex, "Council final-only recovery was canceled for model {ModelName} in phase {Phase} because the Council run was canceled.", modelName, phase);
                return (string.Empty, string.Empty, null);
            }
            catch (ProviderStreamRepetitionException ex)
            {
                logger.LogWarning(
                    "Council repetition watchdog stopped final-answer recovery for {ModelName} in {Phase}: period {PeriodTokens} tokens, agreement {Agreement:P1}, observed {ObservedSeconds:0.0}s.",
                    modelName,
                    phase,
                    ex.PeriodTokens,
                    ex.Agreement,
                    ex.ObservedSeconds);
                return (string.Empty, string.Empty, null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Council final-only recovery failed for model {ModelName}, phase {Phase}, message count {MessageCount}, max output {MaxOutputTokens}.", modelName, phase, originalMessages.Count, maxOutputTokens);
                return (string.Empty, string.Empty, null);
            }

        }

        /// <summary>
        /// Performs multi model council service add ordered step as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        public void MultiModelCouncilServiceAddOrderedStep(MultiModelCouncilResult result, MultiModelCouncilStep step, ILogger logger)
        {
            try
            {
                step.SortOrder = result.Steps.Count;
                if (step.CouncilMembers.Count == 0)
                    step.CouncilMembers = result.ModelNames.ToList();
                result.Steps.Add(step);
                councilSpooler.AddStep(result.RunId, step);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "AddOrderedStep");
            }
        }

        /// <summary>
        /// Performs multi model council service select consensus content as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="consensusStep">Consensus step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceSelectConsensusContent(MultiModelCouncilResult result, MultiModelCouncilStep consensusStep , ILogger logger)
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
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "SelectConsensusContent");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs multi model council service is substantive council content as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool MultiModelCouncilServiceIsSubstantiveCouncilContent(string content, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                var trimmed = content.Trim();
                if (trimmed.Length < 80)
                    return false;

                var letterCount = trimmed.Count(char.IsLetter);
                var wordCount = catalog.WordPattern.Matches(trimmed).Count;
                return letterCount >= 40 && wordCount >= 10;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "IsSubstantiveCouncilContent");
                return false;
            }
        }

        /// <summary>
        /// Performs multi model council service is thinking only council content as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="content">Content value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool MultiModelCouncilServiceIsThinkingOnlyCouncilContent(string content, ILogger logger)
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
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "IsThinkingOnlyCouncilContent");
                return false;
            }
        }
        /// <summary>
        /// Performs multi model council service get council keep alive as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="participantCount">Participant count value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="maxParallelModels">Max parallel models value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceGetCouncilKeepAlive(MultiModelCouncilRequest request, int participantCount, int maxParallelModels, ILogger logger)
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
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "GetCouncilKeepAlive");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs multi model council service should unload after participant as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="keepAlive">Keep alive value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool MultiModelCouncilServiceShouldUnloadAfterParticipant(string keepAlive, ILogger logger)
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

        /// <summary>
        /// Performs multi model council service resolve participant Ollama num GPU as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="requestedNumGpu">Requested num gpu value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The int produced by the operation.</returns>
        public int? MultiModelCouncilServiceResolveParticipantOllamaNumGpu(string modelName, int? requestedNumGpu, ILogger logger)
        {
            try
            {
                // An explicit run/road value remains authoritative. Otherwise leave num_gpu unset so
                // Ollama can choose the appropriate GPU placement for the exact model and host.
                // Family-name heuristics previously forced qwen/gwen/gemma models to a fixed 20-layer
                // partial offload, which also caught low-B variants and could make them slower than
                // Ollama's own placement on adequately sized GPUs.
                return requestedNumGpu;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "ResolveParticipantOllamaNumGpu");
                return null;
            }

        }

        /// <summary>
        /// Performs multi model council service is heavy GPU risk model as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="modelName">Model name value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
        public bool MultiModelCouncilServiceIsHeavyGpuRiskModel(string modelName, ILogger logger)
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


        /// <summary>
        /// Performs multi model council service probe running model names as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="http">Http client dependency used by the multi model council workflow to provide the corresponding application capability.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The hash set string produced by the operation.</returns>
        public async Task<HashSet<string>> MultiModelCouncilServiceProbeRunningModelNamesAsync(HttpClient http, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var running = await http.GetFromJsonAsync<OllamaTagsResponse>("/api/ps", cancellationToken).ConfigureAwait(false) ?? new OllamaTagsResponse();
                return running.Models
                    .Select(model => model.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name!.Trim())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ProbeRunningModelNamesAsync http {http.ToString()}.");
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Loads continuation conversation as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversationId">Identifier of the conversation to use for this operation.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The chat memory conversation snapshot produced by the operation.</returns>
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

        /// <summary>
        /// Performs multi model council service build continuation context as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="conversation">Conversation value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceBuildContinuationContext(ChatMemoryConversationSnapshot? conversation, ILogger logger)
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
                        .AppendLine(councilText.MultiModelCouncilServiceTrimCouncilText(councilText.MultiModelCouncilServiceStripThinking(message.Content, logger), 700, logger));
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

        /// <summary>
        /// Performs multi model council service append prompt section as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="existing">Existing value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="title">Title value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="content">Content value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceAppendPromptSection(string existing, string title, string content, ILogger logger)
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

        /// <summary>
        /// Persists to memory as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="continuedConversation">Continued conversation value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>The GUID produced by the operation.</returns>
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
                        councilText.MultiModelCouncilServiceBuildPollMarkdown(result.UserPoll, logger),
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
                        councilText.MultiModelCouncilServiceBuildArtifactsMarkdown(result.Artifacts, logger),
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
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "SaveToMemoryAsync");
                return null;
            }

        }

        /// <summary>
        /// Performs multi model council service build council request memory message as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="request">Request containing the caller-supplied values that control this operation.</param>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="isContinuation">Value indicating whether is continuation should apply to this operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceBuildCouncilRequestMemoryMessage(
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
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildCouncilRequestMemoryMessage");
                return string.Empty;
            }
        }

        /// <summary>
        /// Performs multi model council service build memory message as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="step">Step value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public string MultiModelCouncilServiceBuildMemoryMessage(MultiModelCouncilStep step, ILogger logger)
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
                        .AppendLine("<details class=\"model-thinking open\" open>")
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

        /// <summary>
        /// Writes log as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        public async Task<string> WriteLogAsync(MultiModelCouncilResult result, CancellationToken cancellationToken, ILogger logger)
        {
            string? temporaryPath = null;
            try
            {
                // CouncilLogs is a diagnostic/audit artifact, not optional model work. Once a run has
                // started, transport/UI cancellation must not cancel the tiny local write that records
                // what happened. Keep the token in the API for compatibility but deliberately do not
                // apply it to the durable write.
                _ = cancellationToken;
                var directory = LocalGptApplicationDataPaths.ResolveUserPath("CouncilLogs");
                Directory.CreateDirectory(directory);

                var path = string.IsNullOrWhiteSpace(result.LogPath)
                    ? Path.Combine(directory, $"council-{DateTime.Now:yyyyMMdd-HHmmss}-{result.RunId:N}.md")
                    : result.LogPath;
                temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
                await System.IO.File.WriteAllTextAsync(
                    temporaryPath,
                    councilRuntime.MultiModelCouncilServiceBuildLogMarkdown(result, logger),
                    CancellationToken.None).ConfigureAwait(false);
                System.IO.File.Move(temporaryPath, path, overwrite: true);
                temporaryPath = null;
                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "WriteLogAsync");
                return string.Empty;
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(temporaryPath))
                {
                    try
                    {
                        if (System.IO.File.Exists(temporaryPath))
                            System.IO.File.Delete(temporaryPath);
                    }
                    catch (Exception cleanupException)
                    {
                        logger.LogDebug(cleanupException, "Could not remove temporary Council log file {TemporaryPath}.", temporaryPath);
                    }
                }
            }
        }

        /// <summary>
        /// Writes missing feature report as part of the multi model council service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Result value supplied to the multi model council operation and used when producing its result.</param>
        /// <param name="cancellationToken">Cancellation token that allows the caller to stop the asynchronous operation.</param>
        /// <returns>A task that completes when the operation has finished.</returns>
        private async Task WriteMissingFeatureReportAsync(
            MultiModelCouncilResult result,
            CancellationToken cancellationToken)
        {
            try
            {
                var reportSource = $"AI Council {result.RunId:N}";
                var reportContent = councilRuntime.MultiModelCouncilServiceBuildLogMarkdown(result, logger);
                var reportPath = await featureReports
                    .WriteIfMissingFeatureReportAsync(reportSource, reportContent, cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(reportPath))
                {
                    logger.LogInformation(
                        "Council run {RunId} wrote its durable missing-feature report to {ReportPath}.",
                        result.RunId,
                        reportPath);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Council run {RunId} could not write its missing-feature report; generated content was omitted from logs.",
                    result.RunId);
            }
        }

  


    
    }
}
