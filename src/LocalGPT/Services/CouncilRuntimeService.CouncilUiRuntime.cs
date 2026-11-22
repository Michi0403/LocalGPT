using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LocalGPT.Services
{
    /// <summary>
    /// Coordinates council runtime behavior for the application, centralizing the workflow, policy, and diagnostics needed by its callers.
    /// </summary>
    public sealed partial class CouncilRuntimeService
    {
    /// <summary>Executes the multi model council service build log markdown operation.</summary>
        /// <summary>
        /// Performs multi model council service build log markdown as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public string MultiModelCouncilServiceBuildLogMarkdown(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine($"# AI Council {result.RunId}")
                .AppendLine()
                .AppendLine($"Started: {result.StartedAtUtc:u}")
                .AppendLine(result.CompletedAtUtc == default ? "Completed: in progress" : $"Completed: {result.CompletedAtUtc:u}")
                .AppendLine($"Models: {string.Join(", ", result.ModelNames)}")
                .AppendLine(result.KnowledgeEntryId is Guid knowledgeId ? $"Knowledge entry: {knowledgeId}" : "Knowledge entry: not saved")
                .AppendLine()
                .AppendLine("## Original Prompt / User Request Audit")
                .AppendLine()
                .AppendLine("This is the exact prompt LocalGPT sent into the AI Council, including the reconstructed DXAiChat conversation when the run came from the chat window.")
                .AppendLine()
                .AppendLine(result.Prompt)
                .AppendLine();

                builder.AppendLine("## Model Selection Audit").AppendLine();
                if (result.ModelSelectionReasons.Count == 0)
                    builder.AppendLine("Model selection provenance was unavailable for this run.");
                else
                    foreach (var reason in result.ModelSelectionReasons)
                        builder.AppendLine($"- {reason}");
                builder.AppendLine();

                if (result.ChatRunId is Guid chatRunId)
                    builder.AppendLine($"Originating chat run: {chatRunId}").AppendLine();

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
                        .AppendLine($"Started: {step.StartedAtUtc:u}")
                        .AppendLine($"Completed: {step.CompletedAtUtc:u}")
                        .AppendLine();

                    if (!string.IsNullOrWhiteSpace(step.FileWorkInstruction))
                        builder.AppendLine("<pre><code>" + System.Net.WebUtility.HtmlEncode(step.FileWorkInstruction) + "</code></pre>").AppendLine();
                    if (!string.IsNullOrWhiteSpace(step.FileWorkReceiptJson))
                        builder.AppendLine("<pre><code>" + System.Net.WebUtility.HtmlEncode(step.FileWorkReceiptJson) + "</code></pre>").AppendLine();

                    if (!string.IsNullOrWhiteSpace(step.Thinking))
                        builder.AppendLine("#### Visible model thinking").AppendLine().AppendLine(step.Thinking).AppendLine();

                    builder.AppendLine(step.VisibleContent).AppendLine();
                }

                if (result.UserPoll is not null)
                {
                    builder.AppendLine("## User Decision Poll").AppendLine().AppendLine(text.MultiModelCouncilServiceBuildPollMarkdown(result.UserPoll, logger)).AppendLine();
                }

                builder.AppendLine("## Final Answer").AppendLine().AppendLine(result.FinalAnswer).AppendLine();

                if (result.Artifacts.Count > 0)
                {
                    builder.AppendLine("## Artifacts").AppendLine().AppendLine(text.MultiModelCouncilServiceBuildArtifactsMarkdown(result.Artifacts, logger)).AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildLogMarkdown");
                return string.Empty;
            }
        }

        /// <summary>Executes the multi model council service build user poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="promptConfig">Provides the persisted follow-up prompts.</param>
        /// <param name="cancellationToken">Cancellation token for prompt retrieval.</param>
        /// <returns>The operation result.</returns>
        public async Task<CouncilUserPoll?> MultiModelCouncilServiceBuildUserPollAsync(MultiModelCouncilResult result, IPromptConfigService promptConfig, ILogger logger, CancellationToken cancellationToken)
        {
            try
            {
                var failedModels = result.Steps
              .Where(step => !string.IsNullOrWhiteSpace(step.Error))
              .Select(step => step.ModelName)
              .Distinct(StringComparer.OrdinalIgnoreCase)
              .ToList();

                var promptLooksFrustrated = MultiModelCouncilServiceIsFrustratedPrompt(result.Prompt, logger);
                var needsAiHostSetupDecision = MultiModelCouncilServiceNeedsAiHostSetupDecision(result, logger);
                var needsImplementationPathDecision = MultiModelCouncilServiceNeedsImplementationPathDecision(result, logger);

                if (failedModels.Count == 0 && !promptLooksFrustrated && !needsAiHostSetupDecision && !needsImplementationPathDecision)
                    return null;

                var prompts = (await promptConfig.ListPromptsAsync("en", cancellationToken).ConfigureAwait(false))
                    .Where(prompt => prompt.Key.StartsWith("CouncilPoll", StringComparison.Ordinal))
                    .GroupBy(prompt => prompt.Key, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.MaxBy(prompt => prompt.LastUpdated)!.Text, StringComparer.Ordinal);

                if (promptLooksFrustrated)
                    return MultiModelCouncilServiceBuildFrustrationPoll(result, failedModels, prompts, logger);

                if (needsAiHostSetupDecision)
                    return MultiModelCouncilServiceBuildAiHostSetupPoll(result, failedModels, prompts, logger);

                if (needsImplementationPathDecision)
                    return MultiModelCouncilServiceBuildImplementationPathPoll(result, failedModels, prompts, logger);

                if (failedModels.Count == 0)
                    return null;

                var reason = $"The council could not fully sync because these participant(s) failed or were unavailable: {string.Join(", ", failedModels)}.";

                var options = new List<CouncilUserPollOption>();
                if (failedModels.Count > 0)
                {
                    options.Add(new CouncilUserPollOption
                    {
                        Label = "Exclude faulty members",
                        Kind = CouncilUserPollOptionKind.ExcludeUnavailableMembers,
                        FollowUpPrompt = string.Format(System.Globalization.CultureInfo.InvariantCulture, prompts.GetValueOrDefault("CouncilPollExcludeUnavailable", string.Empty), string.Join(", ", failedModels))
                    });
                }

                options.AddRange(
                [
                    new CouncilUserPollOption
                {
                    Label = "Wait and retry missing models",
                    FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollRetryAll", string.Empty)
                },
                new CouncilUserPollOption
                {
                    Label = "Proceed with available models",
                    FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollContinueAvailable", string.Empty)
                },
                new CouncilUserPollOption
                {
                    Label = "Ask a shorter tie-break question",
                    FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollAskFollowUp", string.Empty)
                }
                ]);

                return new CouncilUserPoll
                {
                    Question = "How should the AI Council continue so every model stays aligned with your decision?",
                    Reason = reason,
                    Options = options
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildUserPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build implementation path poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="prompts">Persisted follow-up prompts for the poll options.</param>
        /// <returns>The operation result.</returns>
        private CouncilUserPoll? MultiModelCouncilServiceBuildImplementationPathPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, IReadOnlyDictionary<string, string> prompts, ILogger logger)
        {
            try
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
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollAskArchitecture", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Sandbox prototype first",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollPrototype", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use repository default",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollUseRepositoryStack", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Target-specific stack",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollUseRequestedStack", string.Empty)
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildImplementationPathPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build ai host setup poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="prompts">Persisted follow-up prompts for the poll options.</param>
        /// <returns>The operation result.</returns>
        private CouncilUserPoll? MultiModelCouncilServiceBuildAiHostSetupPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, IReadOnlyDictionary<string, string> prompts, ILogger logger)
        {
            try
            {

                var missingModelNote = failedModels.Count > 0
                    ? $" Some participant(s) also failed or were unavailable: {string.Join(", ", failedModels)}."
                    : string.Empty;

                return new CouncilUserPoll
                {
                    Question = "Which native model-runner setup should the AI host artifact use next?",
                    Reason = "The council generated the sandbox AI-host artifact, but local model execution still needs concrete setup choices. " +
                        "This is not a missing-model problem; it is the runner and model-file contract that must be selected before real inference can be proven." +
                        missingModelNote,
                    Options =
                    [
                        new CouncilUserPollOption
                    {
                        Label = "Use llama.cpp GGUF",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollNativeRunner", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use Python.NET runner",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollPythonRunner", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Keep setup-needed",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollSetupNeeded", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Custom runner contract",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollAskRunner", string.Empty)
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildAiHostSetupPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service build frustration poll operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="failedModels">Input value for failedModels.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <param name="prompts">Persisted follow-up prompts for the poll options.</param>
        /// <returns>The operation result.</returns>
        private CouncilUserPoll? MultiModelCouncilServiceBuildFrustrationPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, IReadOnlyDictionary<string, string> prompts, ILogger logger)
        {
            try
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
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollStabilize", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Implement missing feature",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollIdentifyMissingFeature", string.Empty)
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Reduce scope",
                        FollowUpPrompt = prompts.GetValueOrDefault("CouncilPollReduceScope", string.Empty)
                    }
                    ]
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "BuildFrustrationPoll");
                return null;
            }

        }

        /// <summary>Executes the multi model council service is frustrated prompt operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceIsFrustratedPrompt(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                var currentUserText = MultiModelCouncilServiceCurrentUserTextForSignalClassification(prompt, logger);
                var phraseMarkers = new[]
                {
                    "does not work",
                    "doesn't work",
                    "geht nicht",
                    "funktioniert nicht"
                };
                if (phraseMarkers.Any(marker => currentUserText.Contains(marker, StringComparison.OrdinalIgnoreCase)))
                    return true;

                var wordMarkers = new[]
                {
                    "angry",
                    "mad",
                    "frustrated",
                    "annoyed",
                    "upset",
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
                    "scheisse",
                    "scheiße"
                };

                var markerPatternTemplate = runtimePolicy.GetString("CouncilMarkerBoundaryPatternTemplate");
                return wordMarkers.Any(marker => regexCompilation.Compile(
                    markerPatternTemplate.Replace("{{Marker}}", System.Text.RegularExpressions.Regex.Escape(marker), StringComparison.Ordinal),
                    "i,c",
                    runtimePolicy.RegexTimeout,
                    "CouncilMarkerBoundaryPatternTemplate").IsMatch(currentUserText));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify prompt frustration state.");
                return false;
            }

        }

        /// <summary>
        /// Performs multi model council service current user text for signal classification as part of the council runtime service workflow, applying the service's runtime policy, state management, and diagnostics as required.
        /// </summary>
        /// <param name="prompt">Prompt value supplied to the council runtime operation and used when producing its result.</param>
        /// <param name="logger">Logger used to record diagnostics produced while the operation runs.</param>
        /// <returns>The string produced by the operation.</returns>
        private string MultiModelCouncilServiceCurrentUserTextForSignalClassification(string prompt, ILogger logger)
        {
            try
            {
                var currentUserText = prompt;
                var userMarkerIndex = prompt.LastIndexOf("\nUser:", StringComparison.OrdinalIgnoreCase);
                if (userMarkerIndex >= 0)
                    currentUserText = prompt[(userMarkerIndex + "\nUser:".Length)..];

                var priorTranscriptIndex = currentUserText.IndexOf("\nPrior transcript:", StringComparison.OrdinalIgnoreCase);
                if (priorTranscriptIndex >= 0)
                    currentUserText = currentUserText[..priorTranscriptIndex];

                return currentUserText.Trim();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not isolate the latest user text for council signal classification.");
                return prompt;
            }
        }

        /// <summary>Executes the multi model council service needs implementation path decision operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceNeedsImplementationPathDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (!MultiModelCouncilServiceIsDevelopmentRequest(result.Prompt, logger))
                    return false;

                if (MultiModelCouncilServiceHasExplicitArtifactIntent(result.Prompt, logger))
                    return false;

                var text = result.Prompt;
                if (catalog.ImplementationDecisionPattern.IsMatch(text))
                    return true;

                var areaHits = MultiModelCouncilServiceCountImplementationAreaHits(text, logger);
                return areaHits >= 3 && catalog.ImplementationChoicePattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "NeedsImplementationPathDecision");
                return false;
            }

        }

        /// <summary>Executes the multi model council service needs ai host setup decision operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceNeedsAiHostSetupDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var text = result.Prompt;
                if (!catalog.AiHostSetupPattern.IsMatch(text))
                    return false;

                return text.Contains("setup needed", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("native runner executable", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("runner path", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model-file format", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model file format", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "NeedsAiHostSetupDecision");
                return false;
            }
        }

        /// <summary>Executes the multi model council service is development request operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceIsDevelopmentRequest(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.DevelopmentRequestPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify a development request.");
                return false;
            }
        }

        /// <summary>Executes the multi model council service has explicit artifact intent operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceHasExplicitArtifactIntent(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.ExplicitArtifactIntentPattern.IsMatch(prompt) ||
                    catalog.ConcreteMinecraftArtifactPattern.IsMatch(prompt) ||
                    catalog.ConcreteDotNetArtifactPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify explicit artifact intent.");
                return false;
            }
        }


        /// <summary>Executes the multi model council service requires user decision before artifacts operation.</summary>
        /// <param name="result">Input value for result.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceRequiresUserDecisionBeforeArtifacts(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (MultiModelCouncilServiceUserGrantedSafeSandboxChoice(result.Prompt, logger) || MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(result.Prompt, logger))
                    return false;

                var text = $"{result.Prompt} {result.FinalAnswer}";
                return catalog.BlockingArtifactDecisionPattern.IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "RequiresUserDecisionBeforeArtifacts");
                return false;
            }

        }

        /// <summary>Executes the multi model council service user granted safe sandbox choice operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceUserGrantedSafeSandboxChoice(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return catalog.SafeSandboxConsentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify sandbox consent.");
                return false;
            }

        }

        /// <summary>Executes the multi model council service should generate safe sandbox artifact without blocking operation.</summary>
        /// <param name="prompt">Input value for prompt.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public bool MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                if (catalog.ExplicitDoNotGenerateUntilUserDecisionPattern.IsMatch(prompt))
                    return false;

                return MultiModelCouncilServiceHasExplicitArtifactIntent(prompt, logger) ||
                    catalog.DeveloperExecutionIntentPattern.IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Could not classify safe sandbox artifact generation intent.");
                return false;
            }

        }

        /// <summary>Executes the multi model council service count implementation area hits operation.</summary>
        /// <param name="text">Input value for text.</param>
        /// <param name="logger">Input value for logger.</param>
        /// <returns>The operation result.</returns>
        public int MultiModelCouncilServiceCountImplementationAreaHits(string text, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CountImplementationAreaHits text {text?.ToString()}");
                return -1;
            }

        }

    }
}
