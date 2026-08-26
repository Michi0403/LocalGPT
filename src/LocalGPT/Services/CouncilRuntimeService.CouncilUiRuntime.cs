using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
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
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildUserPoll(MultiModelCouncilResult result, ILogger logger)
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

                if (promptLooksFrustrated)
                    return MultiModelCouncilServiceBuildFrustrationPoll(result, failedModels, logger);

                if (needsAiHostSetupDecision)
                    return MultiModelCouncilServiceBuildAiHostSetupPoll(result, failedModels, logger);

                if (needsImplementationPathDecision)
                    return MultiModelCouncilServiceBuildImplementationPathPoll(result, failedModels, logger);

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
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildImplementationPathPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildAiHostSetupPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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
                        FollowUpPrompt = "Continue the same generated AI-host workspace using a user-approved llama.cpp style runner executable boundary and GGUF model files. Add settings for NativeRunnerExecutable, ModelSearchRoots, context size, GPU/layer policy, and per-model session scheduling. Keep no upstream AI-host proxy fallback."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Use Python.NET runner",
                        FollowUpPrompt = "Continue the same generated AI-host workspace with a Python.NET runner boundary. Require user-approved Python runtime path, PYTHONNET_PYDLL, package list, model roots, and a safe backend service contract. Keep the UI in .NET/DevExpress and do not execute unapproved Python code."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Keep setup-needed",
                        FollowUpPrompt = "Keep the artifact buildable and explicit with Setup Needed banners, no proxy fallback, provider-compatible API routes, SQLite settings, and clear user instructions. Do not pretend native inference works until a runner executable and compatible model-file format are supplied."
                    },
                    new CouncilUserPollOption
                    {
                        Label = "Custom runner contract",
                        FollowUpPrompt = "Ask the user for a custom native runner executable, model-file format, arguments, streaming protocol, cancellation behavior, and hardware policy, then continue the generated workspace with those exact choices."
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
        /// <returns>The operation result.</returns>
        public CouncilUserPoll? MultiModelCouncilServiceBuildFrustrationPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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

                return wordMarkers.Any(marker => System.Text.RegularExpressions.Regex.IsMatch(
                    currentUserText,
                    $@"(?<![\p{{L}}\p{{N}}_]){System.Text.RegularExpressions.Regex.Escape(marker)}(?![\p{{L}}\p{{N}}_])",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant));
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
