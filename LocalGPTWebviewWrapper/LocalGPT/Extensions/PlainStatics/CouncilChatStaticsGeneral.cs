using DevExpress.CodeParser;
using DevExpress.Xpo;
using DevExpress.XtraCharts;
using DevExpress.XtraRichEdit.Import.Html;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.AI;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Security.AccessControl;
using System.ServiceModel.Channels;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static DevExpress.Xpo.Helpers.AssociatedCollectionCriteriaHelper;
using static LocalGPT.Endpoints.MinecraftDiagnosticController;
using static LocalGPT.Extensions.PlainStatics.GlobalVariableSlopCollectionToRemove;

namespace LocalGPT.Extensions.PlainStatics
{
    public static class CouncilChatStaticsGeneral
    {
        public static string BuildPrompt(IEnumerable<ChatMessage> messages, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
               .AppendLine("Answer this DXAiChat conversation as the LocalGPT AI Council.")
               .AppendLine("Use the selected members, preserve user intent, and include a concise consensus.")
               .AppendLine();

                foreach (var message in messages.Where(message => message.Role != ChatRole.System))
                {
                    var text = message.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    builder
                        .Append(message.Role == ChatRole.Assistant ? "Assistant" : "User")
                        .AppendLine(":")
                        .AppendLine(text)
                        .AppendLine();
                }

                var prompt = builder.ToString().Trim();
                return prompt.Length <= MaxDxAiChatPromptCharacters
                    ? prompt
                    : prompt[^MaxDxAiChatPromptCharacters..];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildPrompt messages {messages.ToString()}");
                return string.Empty;
            }
        }
        public static string FormatStepProgress(MultiModelCouncilStep step, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
                .AppendLine()
                .AppendLine("<p class=\"localgpt-stream-status\"><em>")
                .Append(WebUtility.HtmlEncode($"{step.ModelName} finished {step.Phase} / {step.Role} in {step.DurationSeconds:n1}s. Step details were streamed above; final consensus appears below."))
                .AppendLine("</em></p>")
                .AppendLine();

                if (!string.IsNullOrWhiteSpace(step.Error))
                {
                    builder
                        .AppendLine($"<details class=\"council-step\" open>")
                        .Append("<summary>")
                        .Append(WebUtility.HtmlEncode($"{step.ModelName} error during {step.Phase}"))
                        .AppendLine("</summary>")
                        .AppendLine()
                        .AppendLine("**Error:**")
                        .AppendLine()
                        .AppendLine(step.Error.Trim())
                        .AppendLine()
                        .AppendLine("</details>")
                        .AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FormatStepProgress step {step.ToString()}");
                return string.Empty;
            }
        }
        public static ChatResponseUpdate? CreateUpdate(string text, ILogger logger)
        {
            try
            {

                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateUpdate text {text.ToString()}");
                return null;
            }
        }
        public static string GetDefaultDatabasePath(ILogger? logger = null)
        {
            try
            {
                return Path.Combine(
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
               "LocalGPT",
               "localgpt-memory.db");
            }
            catch (Exception ex)
            {
                if(logger is not null)
                {
                    logger.LogError(ex, $"Error in GetDefaultDatabasePath");
                }
                else
                {
                    Console.WriteLine($"Error in GetDefaultDatabasePath {ex.ToString()}");
                }
                return string.Empty;
            }
        }
        public static AnalyzedUploadFile? BuildSummary(
    string relativePath,
    long length,
    string kind,
    bool includedInPrompt,
    string note,
    string excerpt, ILogger logger)
        {
            try
            {
                return new AnalyzedUploadFile(
                new ChatUploadWorkspaceFileSummary(
                    relativePath,
                    kind,
                    length,
                    DateTime.UtcNow,
                    includedInPrompt,
                    note),
                CouncilChatStringFunctions.TrimForPrompt(excerpt, MaxExcerptCharactersPerFile, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildSummary relativePath {relativePath} length {length} kind {kind} includedInPrompt {includedInPrompt} note {note} excerpt {excerpt}");
                return null;
            }
        }

        public static AnalyzedUploadFile? BuildBinarySummary(
            string relativePath,
            long length,
            string kind,
            bool includedInPrompt,
            string note, ILogger logger)
        {
            try
            {
                return BuildSummary(relativePath, length, kind, includedInPrompt, note, string.Empty, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildBinarySummary relativePath {relativePath} length {length} kind {kind} includedInPrompt {includedInPrompt} note {note}");
                return null;
            }
        }


        public static string SanitizeForPrompt(string text, ILogger logger)
        {
            try
            {
                var userName = Environment.UserName;
                if (!string.IsNullOrWhiteSpace(userName))
                    text = text.Replace(userName, "%USER%", StringComparison.OrdinalIgnoreCase);

                return text.Replace("\0", string.Empty, StringComparison.Ordinal);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SanitizeForPrompt text {text}");
                return string.Empty;
            }
        }
        public static AnalyzedUploadFile? AnalyzeBytes(string relativePath, byte[] bytes, ILogger logger)
        {
            try
            {
                if (IsZip(relativePath, logger))
                {
                    return BuildBinarySummary(
                        relativePath,
                        bytes.Length,
                        "zip",
                        false,
                        "Zip file saved as uploaded. Extracted safe entries are represented separately.", logger);
                }

                var isText = IsTextLike(relativePath, logger) || LooksLikeText(bytes, logger);
                if (isText)
                {
                    var text = CouncilChatStringFunctions.DecodeText(bytes, logger);
                    return BuildSummary(relativePath, bytes.Length, "text", true, "Text excerpt included.", text, logger);
                }

                var extension = Path.GetExtension(relativePath);
                if (BinaryDiagnosticExtensions.Contains(extension))
                {
                    var strings = CouncilChatStringFunctions.ExtractPrintableStrings(bytes, MaxBinaryStringCharacters, logger);
                    var note = extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase)
                        ? "PDB/debug file summarized with printable strings only."
                        : "Binary file summarized with printable strings only.";
                    return BuildSummary(relativePath, bytes.Length, "binary-strings", true, note, strings, logger);
                }

                return BuildBinarySummary(
                    relativePath,
                    bytes.Length,
                    "binary",
                    false,
                    "Binary file saved but not included in prompt context.", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AnalyzeBytes relativePath: {relativePath.ToString()} bytes: {bytes.ToString()}");
                return null;
            }
        }
        public static string BuildContextMarkdown(
            string workspaceName,
            string root,
            string prompt,
            IReadOnlyList<AnalyzedUploadFile> analyzedFiles,
            IReadOnlyList<string> warnings, ILogger logger)
        {
            try
            {
                var builder = new StringBuilder()
              .AppendLine("# LocalGPT Chat Upload Workspace")
              .AppendLine()
              .AppendLine($"Workspace: `{workspaceName}`")
              .AppendLine($"Root path: `{root}`")
              .AppendLine($"Created UTC: {DateTimeOffset.UtcNow:O}")
              .AppendLine()
              .AppendLine("## Prompt")
              .AppendLine(CouncilChatStringFunctions.TrimForPrompt(prompt, 4_000, logger))
              .AppendLine()
              .AppendLine("## AI workflow instructions")
              .AppendLine("- Use this workspace as uploaded user evidence for the current DXAiChat prompt.")
              .AppendLine("- Read files through chat.upload_workspace_* DXAiFunctions instead of asking for huge pasted context.")
              .AppendLine("- Zips are extracted safely; skipped entries are listed as warnings.")
              .AppendLine("- PDB, DLL, EXE, WASM, and other binaries are never executed; only bounded printable strings are shown.")
              .AppendLine("- Generated or edited code belongs in a council artifact workspace, then a refreshed zip download.")
              .AppendLine();

                if (warnings.Count > 0)
                {
                    builder.AppendLine("## Warnings");
                    foreach (var warning in warnings)
                        builder.AppendLine($"- {warning}");
                    builder.AppendLine();
                }

                builder.AppendLine("## Files");
                foreach (var file in analyzedFiles.Select(file => file.Summary))
                {
                    builder
                        .Append("- ")
                        .Append(file.RelativePath)
                        .Append(" (")
                        .Append(file.Kind)
                        .Append(", ")
                        .Append(file.Length)
                        .Append(" bytes): ")
                        .AppendLine(file.Note);
                }

                builder.AppendLine();
                builder.AppendLine("## Extracted context");

                var remainingCharacters = MaxContextCharacters - builder.Length;
                foreach (var file in analyzedFiles.Where(file => file.Summary.IncludedInPrompt))
                {
                    if (remainingCharacters <= 0)
                        break;

                    var excerpt = CouncilChatStringFunctions.TrimForPrompt(file.Excerpt, Math.Min(MaxExcerptCharactersPerFile, remainingCharacters), logger);
                    if (string.IsNullOrWhiteSpace(excerpt))
                        continue;

                    var section = new StringBuilder()
                        .AppendLine()
                        .AppendLine($"### {file.Summary.RelativePath}")
                        .AppendLine($"Kind: {file.Summary.Kind}. {file.Summary.Note}")
                        .AppendLine()
                        .AppendLine("```text")
                        .AppendLine(excerpt)
                        .AppendLine("```")
                        .ToString();

                    if (section.Length > remainingCharacters)
                        section = CouncilChatStringFunctions.TrimForPrompt(section, remainingCharacters, logger);

                    builder.Append(section);
                    remainingCharacters -= section.Length;
                }

                return CouncilChatStringFunctions.TrimForPrompt(builder.ToString(), MaxContextCharacters, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error inBuildContextMarkdown workspaceName {workspaceName} root {root} prompt {prompt} analyzedFiles {analyzedFiles.ToString()} warnings {warnings.ToString()}");
                return string.Empty;
            }

        }

        public static string? ResolveWorkspaceFile(string workspace, string relativePath, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalized))
                    return null;

                var root = Path.GetFullPath(workspace);
                var file = Path.GetFullPath(Path.Combine(root, normalized));
                return IsInsideRoot(root, file, logger) ? file : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error ResolveWorkspaceFile workspace {workspace} relativePath {relativePath}");
                return null;
            }
        }

        public static bool IsInsideRoot(string root, string path, ILogger logger)
        {
            try
            {
                var normalizedRoot = Path.GetFullPath(root)
      .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
      Path.DirectorySeparatorChar;
                var normalizedPath = Path.GetFullPath(path);
                return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsInsideRoot root {root} path {path}");
                return false;
            }
        }

        public static string BuildWorkspaceName(
            string prompt,
            IReadOnlyList<ChatUploadWorkspaceInputFile> files, ILogger logger)
        {
            try
            {
                var source = files.FirstOrDefault()?.Name;
                if (string.IsNullOrWhiteSpace(source))
                    source = prompt;

                var slug = Regex.Replace(source ?? "prompt", "[^A-Za-z0-9]+", "-")
                    .Trim('-')
                    .ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(slug))
                    slug = "prompt";
                if (slug.Length > 24)
                    slug = slug[..24].Trim('-');

                var suffix = Guid.NewGuid().ToString("N")[..8];
                return $"chat-upload-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{slug}-{suffix}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error BuildWorkspaceName prompt {prompt} files {files}");
                return string.Empty;
            }
        }

        public static bool IsZip(string path, ILogger logger)
        {
            try
            {
                return Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsZip path {path}");
                return false;
            }
        }


        public static bool IsTextLike(string path, ILogger logger)
        {
            try
            {
                return TextExtensions.Contains(Path.GetExtension(path));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error IsTextLike path {path}");
                return false;
            }
        }

        public static string DetermineFileKind(string path, ILogger logger)
        {
            try
            {
                if (IsZip(path, logger))
                    return "zip";
                if (IsTextLike(path, logger))
                    return "text";
                return BinaryDiagnosticExtensions.Contains(Path.GetExtension(path))
                    ? "binary-diagnostic"
                    : "binary";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error DetermineFileKind path {path}");
                return string.Empty;
            }
        }

        public static bool LooksLikeText(byte[] bytes, ILogger logger)
        {
            try
            {
                if (bytes.Length == 0)
                    return true;

                var sampleLength = Math.Min(bytes.Length, 8192);
                var controlCount = 0;
                for (var i = 0; i < sampleLength; i++)
                {
                    var value = bytes[i];
                    if (value == 0)
                        return false;

                    if (value < 9 || (value > 13 && value < 32))
                        controlCount++;
                }

                return controlCount <= sampleLength / 20;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error LooksLikeText bytes {bytes.ToString()}");
                return false;
            }

        }
        public static string MultiModelCouncilServiceBuildLogMarkdown(MultiModelCouncilResult result, ILogger logger)
        {
            try
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
                    builder.AppendLine("## User Decision Poll").AppendLine().AppendLine(CouncilChatStringFunctions.MultiModelCouncilServiceBuildPollMarkdown(result.UserPoll, logger)).AppendLine();
                }

                builder.AppendLine("## Final Answer").AppendLine().AppendLine(result.FinalAnswer).AppendLine();

                if (result.Artifacts.Count > 0)
                {
                    builder.AppendLine("## Artifacts").AppendLine().AppendLine(CouncilChatStringFunctions.MultiModelCouncilServiceBuildArtifactsMarkdown(result.Artifacts, logger)).AppendLine();
                }

                return builder.ToString();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildLogMarkdown result {result?.ToString()}");
                return string.Empty;
            }
        }

        public static CouncilUserPoll? MultiModelCouncilServiceBuildUserPoll(MultiModelCouncilResult result, ILogger logger)
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
                logger.LogError(ex, $"Error in BuildUserPoll result {result?.ToString()}");
                return null;
            }

        }

        public static CouncilUserPoll? MultiModelCouncilServiceBuildImplementationPathPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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
                logger.LogError(ex, $"Error in BuildImplementationPathPoll result {result?.ToString()} failedModels {failedModels?.ToString()}");
                return null;
            }

        }

        public static CouncilUserPoll? MultiModelCouncilServiceBuildAiHostSetupPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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
                logger.LogError(ex, $"Error in BuildAiHostSetupPoll result {result?.ToString()} failedModels {failedModels?.ToString()}");
                return null;
            }

        }

        public static CouncilUserPoll? MultiModelCouncilServiceBuildFrustrationPoll(MultiModelCouncilResult result, IReadOnlyList<string> failedModels, ILogger logger)
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
                logger.LogError(ex, $"Error in BuildFrustrationPoll result {result?.ToString()} failedModels {failedModels?.ToString()}");
                return null;
            }

        }

        public static bool MultiModelCouncilServiceIsFrustratedPrompt(string prompt, ILogger logger)
        {
            try
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
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsFrustratedPrompt prompt {prompt?.ToString()}");
                return false;
            }

        }

        public static bool MultiModelCouncilServiceNeedsImplementationPathDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (!MultiModelCouncilServiceIsDevelopmentRequest(result.Prompt, logger))
                    return false;

                if (MultiModelCouncilServiceHasExplicitArtifactIntent(result.Prompt, logger))
                    return false;

                var text = result.Prompt;
                if (ImplementationDecisionPattern().IsMatch(text))
                    return true;

                var areaHits = MultiModelCouncilServiceCountImplementationAreaHits(text, logger);
                return areaHits >= 3 && ImplementationChoicePattern().IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NeedsImplementationPathDecision result {result?.ToString()}");
                return false;
            }

        }

        public static bool MultiModelCouncilServiceNeedsAiHostSetupDecision(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var text = result.Prompt;
                if (!AiHostSetupPattern().IsMatch(text))
                    return false;

                return text.Contains("setup needed", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("native runner executable", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("runner path", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model-file format", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model file format", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NeedsAiHostSetupDecision result {result?.ToString()}");
                return false;
            }
        }

        public static bool MultiModelCouncilServiceIsDevelopmentRequest(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return DevelopmentRequestPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsDevelopmentRequest prompt {prompt?.ToString()}");
                return false;
            }
        }

        public static bool MultiModelCouncilServiceHasExplicitArtifactIntent(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return ExplicitArtifactIntentPattern().IsMatch(prompt) ||
                    ConcreteMinecraftArtifactPattern().IsMatch(prompt) ||
                    ConcreteDotNetArtifactPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in HasExplicitArtifactIntent prompt {prompt?.ToString()}");
                return false;
            }
        }


        public static bool MultiModelCouncilServiceRequiresUserDecisionBeforeArtifacts(MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                if (MultiModelCouncilServiceUserGrantedSafeSandboxChoice(result.Prompt, logger) || MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(result.Prompt, logger))
                    return false;

                var text = $"{result.Prompt} {result.FinalAnswer}";
                return BlockingArtifactDecisionPattern().IsMatch(text);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequiresUserDecisionBeforeArtifacts result {result?.ToString()}");
                return false;
            }

        }

        public static bool MultiModelCouncilServiceUserGrantedSafeSandboxChoice(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return SafeSandboxConsentPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in UserGrantedSafeSandboxChoice prompt {prompt?.ToString()}");
                return false;
            }

        }

        public static bool MultiModelCouncilServiceShouldGenerateSafeSandboxArtifactWithoutBlocking(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                if (ExplicitDoNotGenerateUntilUserDecisionPattern().IsMatch(prompt))
                    return false;

                return MultiModelCouncilServiceHasExplicitArtifactIntent(prompt, logger) ||
                    DeveloperExecutionIntentPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ShouldGenerateSafeSandboxArtifactWithoutBlocking prompt {prompt?.ToString()}");
                return false;
            }

        }

        public static int MultiModelCouncilServiceCountImplementationAreaHits(string text, ILogger logger)
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
        public static async Task OllamaThinkingChatClientEnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                if (response.IsSuccessStatusCode)
                    return;

                var body = await OllamaThinkingChatClientReadErrorBodyAsync(response, cancellationToken, logger);
                var message = string.IsNullOrWhiteSpace(body)
                    ? $"Ollama returned {(int)response.StatusCode} {response.StatusCode}."
                    : $"Ollama returned {(int)response.StatusCode} {response.StatusCode}: {body}";
                throw new HttpRequestException(message, null, response.StatusCode);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in EnsureSuccessOrThrowAsync response {response.ToString()}");

            }
        }

        public static async Task<string> OllamaThinkingChatClientReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(body))
                    return string.Empty;

                return body.Length <= 4000 ? body.Trim() : body[..4000].Trim() + "...";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadErrorBodyAsync response {response.ToString()}");
                return string.Empty;
            }
        }

        public static ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingUpdate(string text, ILogger logger)
        {
            try
            {
                return new(ChatRole.Assistant, [new TextContent(text)]);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingUpdate text {text.ToString()}");
                return null;
            }
        }


        public static ChatResponseUpdate? OllamaThinkingChatClientCreateStreamingStatusUpdate(string text, ILogger logger)
        {
            try
            {
                return OllamaThinkingChatClientCreateStreamingUpdate($"<p class=\"localgpt-stream-status\"><em>{WebUtility.HtmlEncode(text)}</em></p>\n\n", logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CreateStreamingStatusUpdate text {text.ToString()}");
                return null;
            }
        }

        public static void OllamaThinkingChatClientAddHarmonyResponseProtocol(List<OllamaChatMessage> messages, ILogger logger)
        {
            try
            {
                if (messages.Count > 0 &&
          messages[0].Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    if (!messages[0].Content.Contains(HarmonyResponseProtocol, StringComparison.Ordinal))
                        messages[0].Content = $"{HarmonyResponseProtocol}\n\n{messages[0].Content}";

                    return;
                }

                messages.Insert(0, new OllamaChatMessage
                {
                    Role = "system",
                    Content = HarmonyResponseProtocol
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddHarmonyResponseProtocol messages {messages.ToString()}");
            }
        }

        public static OllamaChatMessage? OllamaThinkingChatClientToOllamaMessage(ChatMessage message, ILogger logger)
        {
            try
            {
                return new OllamaChatMessage
                {
                    Role = message.Role == ChatRole.System ? "system"
                      : message.Role == ChatRole.Assistant ? "assistant"
                      : "user",
                    Content = message.Text
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ToOllamaMessage message {message.ToString()}");
                return null;
            }
        }
        public static CommandPolicyDecision? CommandPolicyDecisionAllow(string profile, string reason, ILogger logger)
        {
            try
            {
                return new(true, "Allowed", reason, profile);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CommandPolicyDecisionAllow profile {profile.ToString()} reason {reason.ToString()}");
                return null;
            }
        }
        public static CommandPolicyDecision? CommandPolicyDecisionDenied(string reason, ILogger logger)
        {
            try
            {
                return new(false, "Denied", reason, "Denied");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CommandPolicyDecisionDenied profile {reason}");
                return null;
            }
        }

        public static async Task AppendDevExpressImportsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var importsPath = Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "Components", "_Imports.razor");
                if (!File.Exists(importsPath))
                    return;

                var text = await File.ReadAllTextAsync(importsPath, cancellationToken);
                var imports = GlobalVariableSlopCollectionToRemove.DevExpressImportPattern()
                    .Matches(text)
                    .Select(match => match.Groups["namespace"].Value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (imports.Count == 0)
                    return;

                builder.AppendLine("- Imported DevExpress namespaces in Blazor:");
                foreach (var item in imports)
                    builder.AppendLine($"  - {item}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressImportsAsync builder {builder.ToString()} root {root.ToString()}");
            }

        }

        public static async Task AppendDevExpressRegistrationsAsync(StringBuilder builder, string root, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var programPath = Path.Combine(root, "LocalGPTWebviewWrapper", "LocalGPT", "Program.cs");
                if (!File.Exists(programPath))
                    return;

                var text = await File.ReadAllTextAsync(programPath, cancellationToken);
                var registrations = GlobalVariableSlopCollectionToRemove.DevExpressRegistrationPattern()
                    .Matches(text)
                    .Select(match => match.Value.TrimEnd('('))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value)
                    .ToList();

                if (registrations.Count == 0)
                    return;

                builder.AppendLine("- DevExpress services registered in ASP.NET Core:");
                foreach (var registration in registrations)
                    builder.AppendLine($"  - {registration}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendDevExpressRegistrationsAsync builder {builder.ToString()} root {root.ToString()}");
            }
            
        }

        public static void AppendLoadedDevExpressAssemblies(StringBuilder builder, ILogger logger)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetName())
                .Where(name => name.Name?.StartsWith("DevExpress.", StringComparison.OrdinalIgnoreCase) == true)
                .OrderBy(name => name.Name)
                .Take(30)
                .ToList();

                if (assemblies.Count == 0)
                    return;

                builder.AppendLine("- Loaded DevExpress assemblies:");
                foreach (var assembly in assemblies)
                    builder.AppendLine($"  - {assembly.Name} {assembly.Version}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AppendLoadedDevExpressAssemblies builder {builder.ToString()}");
            }
            
        }

        public static string? FindRepositoryRoot(ILogger logger)
        {
            try
            {
                foreach (var start in new[]
            {
                Directory.GetCurrentDirectory(),
                AppContext.BaseDirectory
            })
                {
                    var directory = new DirectoryInfo(start);
                    while (directory is not null)
                    {
                        if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) ||
                            Directory.Exists(Path.Combine(directory.FullName, ".git")))
                        {
                            return directory.FullName;
                        }

                        directory = directory.Parent;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in FindRepositoryRoot");
                return null;
            }
            
        }
        public static byte[] CreateChatUploadSmokeZip( ILogger logger)
        {
            try
            {
                using var memory = new MemoryStream();
                using (var archive = new ZipArchive(memory, ZipArchiveMode.Create, leaveOpen: true))
                {
                    CouncilChatStaticsGeneral.WriteZipEntry(archive, "WeatherHost/WeatherHost.sln", """
                    Microsoft Visual Studio Solution File, Format Version 12.00
                    # Visual Studio Version 17
                    Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "WeatherHost", "src\WeatherHost\WeatherHost.csproj", "{11111111-1111-1111-1111-111111111111}"
                    EndProject
                    Global
                    EndGlobal
                    """, logger);
                    CouncilChatStaticsGeneral.WriteZipEntry(archive, "WeatherHost/src/WeatherHost/WeatherHost.csproj", """
                    <Project Sdk="Microsoft.NET.Sdk.Web">
                      <PropertyGroup>
                        <TargetFramework>net10.0</TargetFramework>
                        <Nullable>enable</Nullable>
                        <ImplicitUsings>enable</ImplicitUsings>
                      </PropertyGroup>
                    </Project>
                    """, logger);
                    CouncilChatStaticsGeneral.WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Program.cs", """
                    using WeatherHost.Services;

                    var builder = WebApplication.CreateBuilder(args);
                    builder.Services.AddRazorPages();
                    builder.Services.AddServerSideBlazor();
                    builder.Services.AddScoped<WeatherForecastService>();

                    var app = builder.Build();
                    app.MapGet("/api/weather", (WeatherForecastService service) => service.GetForecasts());
                    app.MapBlazorHub();
                    app.MapFallbackToPage("/_Host");
                    app.Run();
                    """, logger);
                    CouncilChatStaticsGeneral.WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Services/WeatherForecastService.cs", """
                    namespace WeatherHost.Services;

                    public sealed class WeatherForecastService
                    {
                        public IReadOnlyList<WeatherForecast> GetForecasts() =>
                        [
                            new(DateOnly.FromDateTime(DateTime.Today), 21, "Clear"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(1)), 18, "Rain"),
                            new(DateOnly.FromDateTime(DateTime.Today.AddDays(2)), 24, "Sunny")
                        ];
                    }

                    public sealed record WeatherForecast(DateOnly Date, int TemperatureC, string Summary);
                    """, logger);
                    CouncilChatStaticsGeneral.WriteZipEntry(archive, "WeatherHost/src/WeatherHost/Pages/Index.razor", """
                    @page "/"
                    @inject WeatherHost.Services.WeatherForecastService Weather

                    <h1>Weather Host</h1>

                    <ul>
                        @foreach (var item in Weather.GetForecasts())
                        {
                            <li>@item.Date: @item.TemperatureC C, @item.Summary</li>
                        }
                    </ul>
                    """, logger);
                }

                return memory.ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"CreateChatUploadSmokeZip");
                return new byte[0];
            }
        }
        public static void WriteZipEntry(ZipArchive archive, string path, string content, ILogger logger)
        {
            try
            {
                var entry = archive.CreateEntry(path, CompressionLevel.SmallestSize);
                using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
                writer.Write(content.Replace("                    ", string.Empty, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"WriteZipEntry archive {archive.ToString()} path {path.ToString()} content {content.ToString()}");
            }
        }

        public static string GetRequestBaseUrl(HttpContext httpContext, ILogger logger)
        {
            try
            {
                var request = httpContext.Request;
                return $"{request.Scheme}://{request.Host}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"GetRequestBaseUrl httpContext {httpContext.ToString()}");
                return string.Empty;
            }
        }


        public static IReadOnlyList<GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceSummary> EnumerateArtifactWorkspaces(string artifactRoot, int take, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(artifactRoot))
                    return [];

                return Directory
                    .EnumerateDirectories(artifactRoot)
                    .Select(path => CouncilChatStaticsGeneral.BuildArtifactWorkspaceSummary(artifactRoot, path, logger))
                    .Where(summary => summary is not null)
                    .Cast<GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceSummary>()
                    .OrderByDescending(summary => summary.LastWriteTimeUtc)
                    .Take(Math.Clamp(take, 1, 100))
                    .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateArtifactWorkspaces artifactRoot {artifactRoot.ToString()} take {take.ToString()}");
                return new List<GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceSummary>();
            }
        }
        public static DatapackReferenceComparison? DatapackReferenceComparisonMissing(string generatedZipPath, string referenceZipPath, string summary, ILogger logger)
        {
            try
            {
               return new(
                    GeneratedZipPath: generatedZipPath,
                    ReferenceZipPath: referenceZipPath,
                    ReferenceExists: System.IO.File.Exists(referenceZipPath),
                    GeneratedFileCount: 0,
                    GeneratedFunctionFileCount: 0,
                    GeneratedPlaceholderCount: 0,
                    ReferenceFileCount: 0,
                    ReferenceFunctionFileCount: 0,
                    ReferencePlaceholderCount: 0,
                    GeneratedHasRootPackMcmeta: false,
                    ReferenceHasRootPackMcmeta: false,
                    ReferenceHasNestedPackMcmeta: false,
                    GeneratedHasLoadTag: false,
                    GeneratedHasTickTag: false,
                    ReferenceHasLoadTag: false,
                    ReferenceHasTickTag: false,
                    CriticalFileCount: 0,
                    PreservedCriticalFileCount: 0,
                    PreservedCriticalFiles: [],
                    ReferencePlaceholderSamples: [],
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Missing generatedZipPath {generatedZipPath.ToString()} referenceZipPath {referenceZipPath.ToString()} summary {summary.ToString()}");
                return null;
            }
        }

        public static GlobalVariableSlopCollectionToRemove.DatapackReferenceComparison? BuildDatapackReferenceComparison(string workspaceRoot, ILogger logger)
        {
            try
            {
                var generatedZip = Directory.Exists(Path.Combine(workspaceRoot, "build"))
      ? Directory.GetFiles(Path.Combine(workspaceRoot, "build"), "*.zip").Order(StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? string.Empty
      : string.Empty;
                var referenceZip = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads",
                    "living_cities.zip");

                if (string.IsNullOrWhiteSpace(generatedZip) || !System.IO.File.Exists(generatedZip))
                {
                    return CouncilChatStaticsGeneral.DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Generated benchmark zip was not found.", logger);
                }

                if (!System.IO.File.Exists(referenceZip))
                {
                    return CouncilChatStaticsGeneral.DatapackReferenceComparisonMissing(
                        generatedZip,
                        referenceZip,
                        "Reference living_cities.zip was not found in Downloads.", logger);
                }

                var generatedEntries = CouncilChatStaticsGeneral.ReadZipFileEntries(generatedZip, logger);
                var referenceEntries = CouncilChatStaticsGeneral.ReadZipFileEntries(referenceZip, logger);
                var normalizedReferenceEntries = referenceEntries
                    .Select(filter => CouncilChatStaticsGeneral.NormalizeReferenceDatapackEntry(filter, logger))
                    .Where(entry => !string.IsNullOrWhiteSpace(entry))
                    .ToArray();

                var generatedSet = generatedEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var normalizedReferenceSet = normalizedReferenceEntries.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var criticalFiles = new[]
                {
                "pack.mcmeta",
                "data/minecraft/tags/function/load.json",
                "data/minecraft/tags/function/tick.json",
                "data/living_cities/function/core/load.mcfunction",
                "data/living_cities/function/core/tick.mcfunction",
                "data/living_cities/function/city/create.mcfunction",
                "data/living_cities/function/citizens/register.mcfunction",
                "data/living_cities/function/ui/status.mcfunction"
            };
                var preservedCriticalFiles = criticalFiles
                    .Where(file => generatedSet.Contains(file) && normalizedReferenceSet.Contains(file))
                    .ToArray();
                var generatedPlaceholders = generatedEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                var referencePlaceholders = referenceEntries
                    .Where(entry => entry.EndsWith(".mcfunction.txt", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                var summary = string.Join(" ", new[]
                {
                $"Generated zip has {generatedEntries.Length} files and {generatedEntries.Count(filter => CouncilChatStaticsGeneral.IsMcFunctionPath(filter,logger))} functions.",
                $"Reference zip has {referenceEntries.Length} files and {referenceEntries.Count(filter => CouncilChatStaticsGeneral.IsMcFunctionPath(filter,logger))} real functions plus {referencePlaceholders.Length} placeholders.",
                "Generated zip has root pack.mcmeta/load/tick tags; reference keeps those under a top-level folder, so it is useful as a design benchmark but less install-ready as a zip."
            });

                return new GlobalVariableSlopCollectionToRemove.DatapackReferenceComparison(
                    GeneratedZipPath: generatedZip,
                    ReferenceZipPath: referenceZip,
                    ReferenceExists: true,
                    GeneratedFileCount: generatedEntries.Length,
                    GeneratedFunctionFileCount: generatedEntries.Count(filter => CouncilChatStaticsGeneral.IsMcFunctionPath(filter, logger)),
                    GeneratedPlaceholderCount: generatedPlaceholders.Length,
                    ReferenceFileCount: referenceEntries.Length,
                    ReferenceFunctionFileCount: referenceEntries.Count(filter => CouncilChatStaticsGeneral.IsMcFunctionPath(filter, logger)),
                    ReferencePlaceholderCount: referencePlaceholders.Length,
                    GeneratedHasRootPackMcmeta: generatedSet.Contains("pack.mcmeta"),
                    ReferenceHasRootPackMcmeta: referenceEntries.Contains("pack.mcmeta", StringComparer.OrdinalIgnoreCase),
                    ReferenceHasNestedPackMcmeta: normalizedReferenceSet.Contains("pack.mcmeta"),
                    GeneratedHasLoadTag: generatedSet.Contains("data/minecraft/tags/function/load.json"),
                    GeneratedHasTickTag: generatedSet.Contains("data/minecraft/tags/function/tick.json"),
                    ReferenceHasLoadTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/load.json"),
                    ReferenceHasTickTag: normalizedReferenceSet.Contains("data/minecraft/tags/function/tick.json"),
                    CriticalFileCount: criticalFiles.Length,
                    PreservedCriticalFileCount: preservedCriticalFiles.Length,
                    PreservedCriticalFiles: preservedCriticalFiles,
                    ReferencePlaceholderSamples: referencePlaceholders.Take(12).ToArray(),
                    Summary: summary);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDatapackReferenceComparison {ex.ToString()} workspaceRoot {workspaceRoot?.ToString()}");
                return null;
            }
        }
        public static string[] ReadZipFileEntries(string zipPath, ILogger logger)
        {
            try
            {
                using var archive = ZipFile.OpenRead(zipPath);
                return archive.Entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
                    .Select(entry => entry.FullName.Replace('\\', '/'))
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ReadZipFileEntries {ex.ToString()} zipPath {zipPath?.ToString()}");
                return new string[0];
            }

        }

        public static string NormalizeReferenceDatapackEntry(string entry, ILogger logger)
        {
            try
            {
                var normalized = entry.Replace('\\', '/').TrimStart('/');
                const string nestedPrefix = "living_cities/";
                return normalized.StartsWith(nestedPrefix, StringComparison.OrdinalIgnoreCase)
                    ? normalized[nestedPrefix.Length..]
                    : normalized;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in NormalizeReferenceDatapackEntry {ex.ToString()} entry {entry?.ToString()}");
                return string.Empty;
            }

        }

        public static bool IsMcFunctionPath(string entry, ILogger logger)
        {
            try
            {
                return entry.EndsWith(".mcfunction", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsMcFunctionPath {ex.ToString()} entry {entry?.ToString()}");
                return false;
            }
        }
        public static GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceSummary? BuildArtifactWorkspaceSummary(string artifactRoot, string workspacePath, ILogger logger)
        {
            try
            {
                var directory = new DirectoryInfo(workspacePath);
                var files = CouncilChatStaticsGeneral.EnumerateWorkspaceTextFiles(workspacePath, 500,logger);
                var zipNames = Directory
                    .EnumerateFiles(artifactRoot, "*.zip", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Where(name => name!.StartsWith(directory.Name, StringComparison.OrdinalIgnoreCase))
                    .Select(name => name!)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceSummary(
                    directory.Name,
                    directory.FullName,
                    directory.LastWriteTimeUtc,
                    files.Count,
                    files.Count(file => file.RelativePath.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)),
                    files.Count(file => file.RelativePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)),
                    zipNames);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"BuildArtifactWorkspaceSummary artifactRoot {artifactRoot.ToString()} workspacePath {workspacePath.ToString()}");
                return null;
            }

        }
        public static List<GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceFileSummary> EnumerateWorkspaceTextFiles(string workspaceRoot, int take, ILogger logger)
        {
            try
            {
                 if (!Directory.Exists(workspaceRoot))
                return [];

            return Directory
                .EnumerateFiles(workspaceRoot, "*", SearchOption.AllDirectories)
                .Where(file => CouncilChatStaticsGeneral.IsSupportedArtifactTextFile(file,logger))
                .Select(path =>
                {
                    var info = new FileInfo(path);
                    return new GlobalVariableSlopCollectionToRemove.ArtifactWorkspaceFileSummary(
                        CouncilChatStringFunctions.ToForwardSlash(Path.GetRelativePath(workspaceRoot, path),logger),
                        info.Length,
                        info.LastWriteTimeUtc);
                })
                .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
                .Take(Math.Clamp(take, 1, 1000))
                .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"EnumerateWorkspaceTextFiles workspaceRoot {workspaceRoot.ToString()} take {take.ToString()}");
                return new();
            }
           
        }
        public static string? ResolveArtifactWorkspace(string artifactRoot, string workspaceName, ILogger logger)
        {
            try
            {
                var safeName = Path.GetFileName(workspaceName);
                if (!string.Equals(workspaceName, safeName, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(safeName))
                {
                    return null;
                }

                var root = Path.GetFullPath(artifactRoot);
                var path = Path.GetFullPath(Path.Combine(root, safeName));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !Directory.Exists(path))
                {
                    return null;
                }

                return path;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile artifactRoot {artifactRoot.ToString()} workspaceName {workspaceName.ToString()}");
                return null;
            }
        }
        public static string? ResolveWorkspaceTextFile(string workspaceRoot, string relativePath, bool allowMissing, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(relativePath))
                    return null;

                var normalizedRelativePath = relativePath.Replace('/', Path.DirectorySeparatorChar);
                if (Path.IsPathRooted(normalizedRelativePath))
                    return null;

                var root = Path.GetFullPath(workspaceRoot);
                var path = Path.GetFullPath(Path.Combine(root, normalizedRelativePath));
                if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ||
                    !CouncilChatStaticsGeneral.IsSupportedArtifactTextFile(path, logger))
                {
                    return null;
                }

                return allowMissing || System.IO.File.Exists(path) ? path : null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ResolveWorkspaceTextFile workspaceRoot {workspaceRoot.ToString()} relativePath {relativePath.ToString()} allowMissing {allowMissing.ToString()}");
                return null;
            }
        }

        public static bool IsSupportedArtifactTextFile(string path, ILogger logger)
        {
            try
            {
                var extension = Path.GetExtension(path);
                return GlobalVariableSlopCollectionToRemove.ArtifactTextExtensions.Contains(extension);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"IsSupportedArtifactTextFile path {path.ToString()}");
                return false;
            }
        }
        public static async Task<IResult> ReadGuidanceDocsAsync(
            IWebHostEnvironment env,
            IReadOnlyList<string> relativePaths,
            string fallbackBriefing,
            CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                var foundFiles = new List<object>();
                var briefing = new StringBuilder();

                foreach (var relativePath in relativePaths)
                {
                    var candidatePaths = new[]
                    {
                    Path.Combine(AppContext.BaseDirectory, relativePath),
                    Path.Combine(env.ContentRootPath, relativePath),
                    Path.Combine(Directory.GetCurrentDirectory(), relativePath)
                }.Distinct(StringComparer.OrdinalIgnoreCase);

                    var path = candidatePaths.FirstOrDefault(System.IO.File.Exists);
                    if (path is null)
                        continue;

                    var text = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
                    foundFiles.Add(new
                    {
                        RelativePath = relativePath.Replace('\\', '/'),
                        SourcePath = path
                    });

                    briefing
                        .Append("# ")
                        .AppendLine(Path.GetFileName(relativePath))
                        .AppendLine()
                        .AppendLine(text.Trim())
                        .AppendLine();
                }

                return Results.Ok(new
                {
                    GuidanceFiles = foundFiles,
                    Briefing = foundFiles.Count > 0
                        ? briefing.ToString().Trim()
                        : fallbackBriefing.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ReadGuidanceDocsAsync env {env.ToString()} relativePaths {relativePaths} fallbackBriefing {fallbackBriefing}",
             env,
             relativePaths);
                return Results.InternalServerError(ex.ToString());
            }
        }
        public static IReadOnlyList<ChatMessage> LimitPromptSize(IReadOnlyList<ChatMessage> messages, ILogger logger, int? forcedMaxPromptCharacters = null)
        {
            try
            {
                var maxPromptCharacters = Math.Clamp(forcedMaxPromptCharacters ?? GlobalVariableSlopCollectionToRemove.DefaultMaxPromptCharacters, 512, GlobalVariableSlopCollectionToRemove.MaxPromptCharacters);
                if (messages.Sum(EstimateTextLength) <= maxPromptCharacters)
                    return messages;

                var result = new List<ChatMessage>();
                var usedCharacters = 0;
                var remainingSystemBudget = Math.Min(GlobalVariableSlopCollectionToRemove.MaxBootstrapCharacters, Math.Max(maxPromptCharacters / 2, 0));

                foreach (var message in messages.Where(message => message.Role == ChatRole.System))
                {
                    var text = message.Text ?? string.Empty;
                    var budget = Math.Min(remainingSystemBudget, maxPromptCharacters - usedCharacters);
                    if (budget <= 0)
                        break;

                    var trimmed = CouncilChatStringFunctions.TrimForPrompt(text, budget,logger, keepBothEnds: false);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    result.Add(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                    remainingSystemBudget -= trimmed.Length;
                }

                var conversationMessages = messages
                    .Where(message => message.Role != ChatRole.System)
                    .ToList();
                var keptConversationMessages = new Stack<ChatMessage>();

                for (var index = conversationMessages.Count - 1; index >= 0; index--)
                {
                    var remainingBudget = maxPromptCharacters - usedCharacters;
                    if (remainingBudget <= 0)
                        break;

                    var message = conversationMessages[index];
                    var text = message.Text ?? string.Empty;
                    var messageBudget = Math.Min(GlobalVariableSlopCollectionToRemove.MaxSingleConversationMessageCharacters, remainingBudget);
                    var trimmed = CouncilChatStringFunctions.TrimForPrompt(text, messageBudget, logger, keepBothEnds: true);
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    keptConversationMessages.Push(new ChatMessage(message.Role, trimmed));
                    usedCharacters += trimmed.Length;
                }

                result.AddRange(keptConversationMessages);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"LimitPromptSize messages {messages.ToString()} forcedMaxPromptCharacters {forcedMaxPromptCharacters}",
             messages,
             forcedMaxPromptCharacters);
                return new List<ChatMessage>();
            }
        }

        public static int EstimateTextLength(ChatMessage message) => message.Text?.Length ?? 0;

        public static bool? TryIsSupportedOllamaMode(string mode,ILogger logger)
        {
            try
            {
                return mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeAutoGpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeSafeCpu, StringComparison.OrdinalIgnoreCase) ||
            mode.Equals(GlobalVariableSlopCollectionToRemove.OllamaModeLimitedGpu, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSupportedOllamaMode mode:{mode}");
                return null;
            }

        }
       
        public static bool? IsBlazorFrontendTarget(string prompt, string finalAnswer, string targetArea, ILogger logger)
        {
            try
            {
                return targetArea.Contains("Blazor/DevExpress frontend", StringComparison.OrdinalIgnoreCase) ||
               GlobalVariableSlopCollectionToRemove.BlazorFrontendPattern().IsMatch($"{prompt} {finalAnswer}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsSupportedOllamaMode prompt:{prompt} finalAnswer:{finalAnswer} targetArea:{targetArea}");
                return null;
            }
           
        }

        public static bool? IsWholeSolutionTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                var isAiHostExperimentTarget = IsAiHostExperimentTarget(prompt, finalAnswer, logger);
                bool isAiHostExperimentTargetBool = isAiHostExperimentTarget ?? false;
                return GlobalVariableSlopCollectionToRemove.WholeSolutionPattern().IsMatch(prompt) || isAiHostExperimentTargetBool;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsWholeSolutionTarget prompt:{prompt} finalAnswer:{finalAnswer}");
                return null;
            }
        }

        public static bool? IsAiHostExperimentTarget(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {

                return GlobalVariableSlopCollectionToRemove.AiHostExperimentPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsAiHostExperimentTarget prompt:{prompt} finalAnswer:{finalAnswer}");
                return null;
            }
        }

        public static bool? IsAdviceOnlyPrompt(string prompt, ILogger logger)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                    return false;

                return GlobalVariableSlopCollectionToRemove.AdviceOnlyPromptPattern().IsMatch(prompt) &&
                    !GlobalVariableSlopCollectionToRemove.ExplicitArtifactCreationCommandPattern().IsMatch(prompt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsAdviceOnlyPrompt prompt:{prompt}");
                return null;
            }
        }

        public static GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype? DetectSolutionArchetype(string prompt, string finalAnswer, ILogger logger)
        {
            try
            {
                if (GlobalVariableSlopCollectionToRemove.AiHostExperimentPattern().IsMatch(prompt))
                    return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.AiHost;
                if (GlobalVariableSlopCollectionToRemove.LocalGptReplacementPattern().IsMatch(prompt))
                    return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt;
                if (GlobalVariableSlopCollectionToRemove.TacosPortalPattern().IsMatch(prompt))
                    return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.TacosPortal;
                if (GlobalVariableSlopCollectionToRemove.BotBackendPattern().IsMatch(prompt))
                    return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.BotBackend;

                return GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.Generic;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in DetectSolutionArchetype prompt:{prompt} finalAnswer:{finalAnswer}");
                return null;
            }
        }


        public static void ValidateGeneratedDatapackWorkspace(string rootPath, ILogger logger)
        {
            try
            {
                var packPath = Path.Combine(rootPath, "pack.mcmeta");
                var dataPath = Path.Combine(rootPath, "data");
                if (!File.Exists(packPath))
                    throw new InvalidOperationException("Generated datapack is missing root pack.mcmeta.");
                if (!Directory.Exists(dataPath))
                    throw new InvalidOperationException("Generated datapack is missing root data folder.");

                JsonDocument.Parse(File.ReadAllText(packPath));
                foreach (var tagPath in Directory.GetFiles(Path.Combine(dataPath, "minecraft", "tags", "function"), "*.json"))
                    JsonDocument.Parse(File.ReadAllText(tagPath));

                var nestedPack = Directory
                    .EnumerateDirectories(rootPath)
                    .Select(directory => Path.Combine(directory, "pack.mcmeta"))
                    .FirstOrDefault(File.Exists);
                if (nestedPack is not null)
                    throw new InvalidOperationException("Generated datapack has a nested wrapper folder containing pack.mcmeta.");

                var pluralFunctionsFolder = Directory
                    .EnumerateDirectories(dataPath, "functions", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (pluralFunctionsFolder is not null)
                    throw new InvalidOperationException("Generated datapack contains legacy plural functions folder; Minecraft 1.21+ uses function.");

                var txtPlaceholder = Directory
                    .EnumerateFiles(dataPath, "*.mcfunction.txt", SearchOption.AllDirectories)
                    .FirstOrDefault();
                if (txtPlaceholder is not null)
                    throw new InvalidOperationException("Generated datapack contains .mcfunction.txt placeholder files.");

                foreach (var functionFile in Directory.EnumerateFiles(dataPath, "*.mcfunction", SearchOption.AllDirectories))
                {
                    var content = File.ReadAllText(functionFile);
                    if (GlobalVariableSlopCollectionToRemove.LeadingSlashCommandPattern().IsMatch(content))
                        throw new InvalidOperationException($"Generated function contains a leading slash command: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (GlobalVariableSlopCollectionToRemove.RootStorageRemovePattern().IsMatch(content))
                        throw new InvalidOperationException($"Generated function uses data remove storage root syntax: {Path.GetRelativePath(rootPath, functionFile)}");
                    if (GlobalVariableSlopCollectionToRemove.MalformedStorageTargetPattern().IsMatch(content))
                        throw new InvalidOperationException($"Generated function appears to put an NBT path into the storage id instead of after it: {Path.GetRelativePath(rootPath, functionFile)}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGeneratedDatapackWorkspace rootPath:{rootPath}");
             
            }
           
        }

        public static void AddDirectoryToZip(ZipArchive archive, string rootPath, string directoryPath, ILogger logger)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return;

                foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
                {
                    var entryName = Path.GetRelativePath(rootPath, filePath).Replace('\\', '/');
                    AddFileToZip(archive, filePath, entryName, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddDirectoryToZip archive:{archive.ToString()} rootPath:{rootPath} directoryPath:{directoryPath}", archive);

            }
        }

        public static void AddFileToZip(ZipArchive archive, string filePath, string entryName, ILogger logger)
        {
            try
            {
                if (!File.Exists(filePath))
                    return;

                archive.CreateEntryFromFile(filePath, entryName.Replace('\\', '/'), CompressionLevel.SmallestSize);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in AddFileToZip archive:{archive.ToString()} filePath:{filePath} entryName:{entryName}", archive);
            }
        }

        public static void CopyDirectory(string sourceDirectory, string destinationDirectory, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(destinationDirectory);
                foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativeDirectory = Path.GetRelativePath(sourceDirectory, directory);
                    Directory.CreateDirectory(Path.Combine(destinationDirectory, relativeDirectory));
                }

                foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    var relativeFile = Path.GetRelativePath(sourceDirectory, file);
                    var destinationFile = Path.Combine(destinationDirectory, relativeFile);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFile)!);
                    File.Copy(file, destinationFile, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyDirectory sourceDirectory:{sourceDirectory} destinationDirectory:{destinationDirectory}");
            }
           
        }

    

        public static GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage? ArchetypePage(
            string fileName,
            string route,
            string title,
            string summary,
            IReadOnlyList<string> areas, ILogger logger)
        {
            try
            {
                return new GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage(
             fileName,
             CouncilChatStringFunctions.GenerateArchetypePageRazor(route, title, summary, areas, logger));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ArchetypePage fileName:{fileName} route:{route} title:{title} summary:{summary} areas:{areas.ToString()}");
                return null;
            }
         
        }
        public static Task WriteTextAsync(string path, string content, CancellationToken cancellationToken, ILogger logger)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path has no directory: {path}"));
                return File.WriteAllTextAsync(path, content, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in WriteTextAsync path:{path} content:{content}");
                return Task.CompletedTask;
            }
        }

        public static IReadOnlyList<GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage> GenerateArchetypePages(GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype archetype, ILogger logger)
        {
            try
            {
                return archetype switch
                {
                    GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt =>
                    [
                        ArchetypePage("Chat.razor", "/chat", "DXAiChat", "Chat surface with model routing, uploads, artifact links, visible progress, and memory-aware continuation.", ["Model selection", "Council mode", "File context", "Artifact downloads"],logger),
                    ArchetypePage("ModelCouncil.razor", "/model-council", "AI Council", "Multi-model review surface for feedback talks, polls, missing features, source requests, and implementation artifacts.", ["Minimum two members", "Sequential scheduling", "Poll gate", "Feedback log"],logger),
                    ArchetypePage("Database.razor", "/database", "SQLite Database", "Editable operational memory for chats, thoughts, logs, knowledge, benchmark scores, and approval markers.", ["CouncilKnowledgeEntries", "ChatMessages", "ApplicationLogs", "BenchmarkResults"],logger),
                    ArchetypePage("MinecraftModBuilder.razor", "/minecraft-mod-builder", "Minecraft Mod Builder", "Workspace generator for datapacks, Fabric, Paper, NeoForge, Java/Gradle setup, validation, and downloads.", ["Datapack zip", "Loader matrix", "Version resolver", "Validation script"],logger),
                    ArchetypePage("TestLab.razor", "/test-lab", "Test Lab", "Frontend-accessible diagnostics for API smoke checks, benchmark routes, artifact downloads, and WebView2 workflows.", ["Health", "DXAiFunctions", "Replacement benchmark", "Council feedback"],logger),
                    ArchetypePage("Install.razor", "/install", "Install", "Model host discovery, Ollama/LM Studio status, model pull planning, runtime checks, and setup guidance.", ["Ollama status", "LM Studio status", "Model downloads", "Java/.NET checks"],logger)
                    ],
                    GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.TacosPortal =>
                    [
                        ArchetypePage("TelegramIngestion.razor", "/telegram-ingestion", "Telegram Ingestion", "Event-ingestion boundary with update handling, command routing, idempotency, retries, and sanitized bot service wiring.", ["Update handler", "Command router", "Idempotency", "Retry queue"],logger),
                    ArchetypePage("Persistence.razor", "/persistence", "Persistence", "Normalized domain persistence with EF/SQLite or provider-specific backend, explicit DTO/service boundaries, and migration notes.", ["Business objects", "DbContext", "DTO boundaries", "Migration safety"],logger),
                    ArchetypePage("Workers.razor", "/workers", "Workers", "Hosted/background worker view for polling, notification dispatch, API synchronization, and operational diagnostics.", ["Hosted services", "Polling", "Notifications", "Diagnostics"],logger),
                    ArchetypePage("Admin.razor", "/admin", "Admin", "DevExpress CRUD/admin workbench with roles, audit log, validation, custom security, and operational settings.", ["Users", "Roles", "Audit", "Settings"],logger),
                    ArchetypePage("ClientShells.razor", "/client-shells", "Client Shells", "Host map for Blazor server, optional WASM client, WinUI/WebView2 wrapper, package boundaries, and debug/deploy notes.", ["Server host", "WASM client", "WinUI/WebView2", "Package diagnostics"],logger)
                    ],
                    GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.BotBackend =>
                    [
                        ArchetypePage("Webhooks.razor", "/webhooks", "Webhooks", "Inbound message and event receiver surface with validation, idempotency, and retry diagnostics.", ["Ingress", "Signature check", "Idempotency", "Dead letters"],logger),
                    ArchetypePage("Conversations.razor", "/conversations", "Conversations", "Conversation-state workbench with memory, moderation, handoff, and compact transcript review.", ["Memory", "Moderation", "Handoff", "Transcript"],logger),
                    ArchetypePage("BotSettings.razor", "/bot-settings", "Bot Settings", "Provider-neutral bot configuration with secrets stored outside the generated code and visible safety gates.", ["Provider", "Token source", "Allowed commands", "Rate limit"],logger),
                    ArchetypePage("PythonInterop.razor", "/python-interop", "Python Interop", "Optional Python.NET or process-adapter boundary for transcription, translation, media, or model tooling.", ["Python.NET", "Process adapter", "Safe directory", "User approval"],logger)
                    ],
                    _ => []
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GenerateArchetypePages archetype:{archetype.ToString()}");
                return new List<GlobalVariableSlopCollectionToRemove.GeneratedArchetypePage>();
            }
           
        }
        public static IReadOnlyList<GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule> ExtractDynamicPromiseModules(
            MultiModelCouncilRequest request,
            MultiModelCouncilResult result, ILogger logger)
        {
            try
            {
                var text = $"{request.Prompt} {result.FinalAnswer}";
                var modules = new List<GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule>();

                void AddIf(bool condition, string title, string summary, IReadOnlyList<string> areas)
                {
                    if (!condition || modules.Any(module => module.Title.Equals(title, StringComparison.OrdinalIgnoreCase)))
                        return;

                    var route = "/" + CouncilChatStringFunctions.ToKebabRoute(title,logger);
                    var fileName = $"{CouncilChatStringFunctions.ToPascalIdentifier(title, logger)}.razor";
                    modules.Add(new GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule(fileName, route, title, summary, areas));
                }

                AddIf(
                    GlobalVariableSlopCollectionToRemove.DevExpressDocumentPattern().IsMatch(text) || GlobalVariableSlopCollectionToRemove.ExportFormatPattern().IsMatch(text),
                    "Document Exports",
                    "Promise-derived surface for report, Office, PDF, spreadsheet, presentation, and document export work owned by backend services.",
                    ["Report template", "Format mapping", "Backend service", "Download route"]);
                AddIf(
                    text.Contains("FileDownloadController", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("download link", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("download route", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("safe download", StringComparison.OrdinalIgnoreCase),
                    "Download Center",
                    "Promise-derived surface for generated files, MIME types, safe HTTP GET links, checksums, expiry, and user-visible artifact status.",
                    ["Generated files", "HTTP GET", "Checksum", "Expiry"]);
                AddIf(
                    text.Contains("DxAiFunctions", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("IAIInferenceProvider", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("/api/inference", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("AI prompt", StringComparison.OrdinalIgnoreCase),
                    "AI Prompt Flow",
                    "Promise-derived surface for prompt-to-plan workflows, model/provider calls, generated briefs, and Needs verification notes.",
                    ["Prompt", "Provider call", "Generated brief", "Verification"]);
                AddIf(
                    text.Contains("IModelCatalogService", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("model catalog", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("LM Studio", StringComparison.OrdinalIgnoreCase),
                    "Model Host Status",
                    "Promise-derived surface for local model/provider inventory, host reachability, selected model, and runtime status.",
                    ["Provider", "Model catalog", "Reachability", "Runtime status"]);
                AddIf(
                    text.Contains("SQLite", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("EF/", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("persist", StringComparison.OrdinalIgnoreCase),
                    "Persistence",
                    "Promise-derived surface for database state, DTO projection, migration safety, audit records, and user-approved knowledge.",
                    ["EF/SQLite", "DTOs", "Migration safety", "Audit"]);
                AddIf(
                    text.Contains("DevExpress", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("DxGrid", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("DxFormLayout", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("Blazor", StringComparison.OrdinalIgnoreCase),
                    "DevExpress UI",
                    "Promise-derived surface for DevExpress Blazor controls, layout, navigation, forms, grids, and frontend verification.",
                    ["Navigation", "Grid", "Form", "Frontend smoke"]);
                AddIf(
                    text.Contains("API endpoint", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("controller", StringComparison.OrdinalIgnoreCase) ||
                    text.Contains("/api/", StringComparison.OrdinalIgnoreCase),
                    "API Contracts",
                    "Promise-derived surface for backend routes, request/response DTOs, validation, errors, and smoke-test calls.",
                    ["Routes", "DTOs", "Validation", "Smoke tests"]);

                return modules.Take(8).ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ExtractDynamicPromiseModules request:{request.ToString()} result:{result.ToString()}");
                return new List<GlobalVariableSlopCollectionToRemove.GeneratedPromiseModule>();
            }
           
        }

        public static List<CouncilKnowledgeEntry> SortKnowledgeEntries(IEnumerable<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                return entries
             .OrderBy(filter => CouncilChatStaticsGeneral.KnowledgeReviewPriority(filter, logger))
             .ThenByDescending(entry => entry.IsPinned)
             .ThenByDescending(entry => entry.IsUserApproved)
             .ThenByDescending(entry => entry.UpdatedAtUtc)
             .ToList();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in SortKnowledgeEntries entries:{entries.ToString()}");
                return new List<CouncilKnowledgeEntry>();
            }

        }
        public static CouncilKnowledgeEntry? CopyKnowledgeEntry(CouncilKnowledgeEntry entry,ILogger logger)
        {
            try
            {
                return new CouncilKnowledgeEntry
                {
                    Id = entry.Id,
                    CreatedAtUtc = entry.CreatedAtUtc,
                    UpdatedAtUtc = entry.UpdatedAtUtc,
                    Topic = entry.Topic,
                    Scope = entry.Scope,
                    Content = entry.Content,
                    Source = entry.Source,
                    HelpfulSources = entry.HelpfulSources,
                    Tags = entry.Tags,
                    Confidence = entry.Confidence,
                    VerificationStatus = entry.VerificationStatus,
                    ReviewStatus = entry.ReviewStatus,
                    ExpiresAtUtc = entry.ExpiresAtUtc,
                    LastVerifiedAtUtc = entry.LastVerifiedAtUtc,
                    LastUsedAtUtc = entry.LastUsedAtUtc,
                    SupersededByKnowledgeId = entry.SupersededByKnowledgeId,
                    StalenessReason = entry.StalenessReason,
                    StalenessDetectedAtUtc = entry.StalenessDetectedAtUtc,
                    StalenessDetectedBy = entry.StalenessDetectedBy,
                    SourceHash = entry.SourceHash,
                    SourceDateUtc = entry.SourceDateUtc,
                    IsUserApproved = entry.IsUserApproved,
                    IsPinned = entry.IsPinned,
                    IsArchived = entry.IsArchived
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in CopyKnowledgeEntry entry:{entry.ToString()}");
                return null;
            }
        }

        public static string BuildKnowledgeReviewSummary(IReadOnlyCollection<CouncilKnowledgeEntry> entries, ILogger logger)
        {
            try
            {
                if (entries.Count == 0)
                    return "No knowledge notes loaded yet.";

                var needsAttention = entries.Count(entry => entry.ReviewStatus is "NeedsUserReview" or "NeedsSourceRefresh" or "NeedsDiagnosticVerification" or "Expired");
                var trusted = entries.Count(entry => entry.ReviewStatus == "Current" && entry.IsUserApproved);
                return $"{needsAttention} note(s) need attention. {trusted} user-approved current note(s) can guide the council.";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildKnowledgeReviewSummary entries:{entries.ToString()}");
                return string.Empty;
            }
            
        }

        public static int? GetCouncilModelLoadPriorityRandomisator(int maxPriority, string modelName, ILogger logger)
        {
            try
            {
                var random = new Random();
                int randomNumber = random.Next(maxPriority);
                logger.LogInformation($"GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} returning value..{randomNumber}");
                return randomNumber;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in GetCouncilModelLoadPriorityRandomisator modelName:{modelName.ToString()} maxPriority:{maxPriority}");
                return null;
            }
            
        }

        public static int? KnowledgeReviewPriority(CouncilKnowledgeEntry entry, ILogger logger)
        {
            try
            {
                return entry.ReviewStatus switch
                {
                    "NeedsUserReview" => 0,
                    "NeedsSourceRefresh" => 1,
                    "NeedsDiagnosticVerification" => 2,
                    "Expired" => 3,
                    "Superseded" => 4,
                    "Deprecated" => 5,
                    "Current" => 6,
                    "Archived" => 7,
                    _ => 8
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in KnowledgeReviewPriority entry:{entry.ToString()}");
                return null;
            }
           
        }

        public static bool? IsDynamicSession(ChatClientSession session, ILogger logger)
        {
            try
            {
                return session.Name.StartsWith(GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix, StringComparison.OrdinalIgnoreCase) ||
            session.Name.Equals(GlobalVariableSlopCollectionToRemove.CouncilSessionName, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in IsDynamicSession sessionm:{session.ToString()}");
                return null;
            }

        }
       

        public static IEnumerable<string>? OrderCouncilModelsForLoad(IEnumerable<string> modelNames, ILogger logger)
        {
            try
            {
                return modelNames
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .OrderBy(filter =>  CouncilChatStaticsGeneral.GetCouncilModelLoadPriorityRandomisator(modelNames.Count(), filter,logger) ?? 0)
               .ThenBy(name => name, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in OrderCouncilModelsForLoad modelNames:{modelNames.ToString()}");
                return null;
            }

        }
        public static string BuildDynamicSessionName(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{GlobalVariableSlopCollectionToRemove.DetectedOllamaSessionPrefix}{candidate.ModelName} @ {CouncilChatStringFunctions.TrimEndpoint(candidate.Endpoint, logger)}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildDynamicSessionName candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
  

        public static string BuildCandidateLabel(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                return $"{candidate.ModelName} @ {CouncilChatStringFunctions.TrimEndpoint(candidate.Endpoint,logger)}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateLabel candidate:{candidate.ToString()}");
                return string.Empty;
            }

        }
           

        public static string BuildCandidateTitle(MultiModelCouncilModelCandidate candidate, ILogger logger)
        {
            try
            {
                var details = string.IsNullOrWhiteSpace(candidate.Details)
          ? "No model details reported."
          : candidate.Details;
                return $"{candidate.Provider} at {candidate.Endpoint}. {details}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in BuildCandidateTitle candidate:{candidate.ToString()}");
                return string.Empty;
            }

      
        }


        public static GlobalVariableSlopCollectionToRemove.ArtifactContractReport? ValidateSolutionArtifactContract(
            string solutionRoot,
            string projectName,
            GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype archetype,ILogger logger)
        {
            try
            {
                var isAiHostLab = archetype == GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.AiHost;
                var requiredFiles = new List<string>
            {
                $"{projectName}.sln",
                "README.md",
                "PROJECT_INDEX.md",
                "ARCHITECTURE.md",
                "SOURCE_FIDELITY.md",
                "BUILD_AND_RUN.md",
                ".localgpt-generation.json",
                "LocalGPT.GenerationManifest.json",
                Path.Combine("src", projectName, $"{projectName}.csproj"),
                Path.Combine("src", projectName, "Program.cs"),
                Path.Combine("src", projectName, "Components", "GeneratedNavigation.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "Index.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedDashboard.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "GeneratedKnowledgeTable.razor"),
                Path.Combine("src", projectName, "Components", "Pages", "SourceFidelity.razor"),
                Path.Combine("src", projectName, "Components", "Pages", isAiHostLab ? "ApiConsole.razor" : "ImplementationPlan.razor"),
                Path.Combine("src", projectName, "Services", "GeneratedHealthSummaryService.cs"),
                Path.Combine("src", projectName, "Services", "GeneratedSourceFidelityService.cs"),
                Path.Combine("src", projectName, "Models", "GeneratedHealthCard.cs"),
                Path.Combine("src", projectName, "wwwroot", "app.css"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "dashboard-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "catalog-solid.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-line.svg"),
                Path.Combine("src", projectName, "wwwroot", "icons", "nav", "detail-solid.svg")
            };

                if (isAiHostLab)
                {
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Chat.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunningModels.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "ModelDownloads.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Templates.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Hardware.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "RunnerPlugins.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Logs.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Components", "Pages", "Settings.razor"));
                    requiredFiles.Add(Path.Combine("src", projectName, "Services", "GeneratedAiHostArchitectureServices.cs"));
                }

                var missing = requiredFiles
                    .Where(relativePath => !File.Exists(Path.Combine(solutionRoot, relativePath)))
                    .ToArray();

                if (missing.Length > 0)
                    throw new InvalidOperationException($"Generated solution artifact is missing required files: {string.Join(", ", missing)}");

                ValidateGenerationContractJson(Path.Combine(solutionRoot, ".localgpt-generation.json"), logger);
                ValidateGenerationManifestJson(Path.Combine(solutionRoot, "LocalGPT.GenerationManifest.json"), logger);

                if (isAiHostLab)
                {
                    ValidateAiHostArtifactContract(solutionRoot, projectName, logger);
                    return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                        "Source-contract prototype",
                        "AI-host source contract validated",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable",
                        "AI-host endpoint and native-runner source markers exist"
                        ],
                        ["No model-file runtime execution proof was produced", "No generated-project build proof was produced"],
                        "AI-host routes, settings, navigation, and native-runner source markers were checked before zipping; runtime behavior is still unproven.");
                }

                if (archetype == GlobalVariableSlopCollectionToRemove.GeneratedSolutionArchetype.LocalGpt)
                {
                    return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                        "Static LocalGPT-style prototype",
                        "Missing LocalGPT runtime contract",
                        [
                            "Required generated file set exists",
                        "Generation contract JSON is parseable",
                        "Generation manifest JSON is parseable"
                        ],
                        [
                            "DXAiChat runtime wiring is not proven",
                        "AI Council execution is not proven",
                        "SQLite memory persistence is not proven",
                        "Artifact route behavior is not proven"
                        ],
                        "LocalGPT-like source files were generated, but the artifact must not be treated as a working LocalGPT replacement.");
                }

                return new GlobalVariableSlopCollectionToRemove.ArtifactContractReport(
                    "Generated solution prototype",
                    "Generated files validated",
                    [
                        "Required generated file set exists",
                    "Generation contract JSON is parseable",
                    "Generation manifest JSON is parseable"
                    ],
                    ["No generated-project build proof was produced", "No runtime UI proof was produced"],
                    "Required files and metadata were checked before zipping; build and runtime behavior are unproven.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateSolutionArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()} archetype:{archetype.ToString()}");
                return null;
            }
        }

        public static void ValidateAiHostArtifactContract(string solutionRoot, string projectName, ILogger logger)
        {
            try
            {
                var projectRoot = Path.Combine(solutionRoot, "src", projectName);
                var programPath = Path.Combine(projectRoot, "Program.cs");
                var architectureServicePath = Path.Combine(projectRoot, "Services", "GeneratedAiHostArchitectureServices.cs");
                var appSettingsPath = Path.Combine(projectRoot, "appsettings.json");
                var navigationPath = Path.Combine(projectRoot, "Components", "GeneratedNavigation.razor");

                var program = File.ReadAllText(programPath);
                var architectureService = File.ReadAllText(architectureServicePath);
                var appSettings = File.ReadAllText(appSettingsPath);
                var navigation = File.ReadAllText(navigationPath);

                var requiredRoutes = new[]
                {
                "/api/version",
                "/api/tags",
                "/api/ps",
                "/api/generate",
                "/api/chat"
            };

                foreach (var route in requiredRoutes)
                {
                    if (!program.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required route {route}.");
                }

                var requiredProgramTokens = new[]
                {
                "IInferenceProvider",
                "NativeModelFileInferenceProvider",
                "IInferenceRunner",
                "NativeModelFileProcessRunner",
                "upstream_proxy = false"
            };

                foreach (var token in requiredProgramTokens)
                {
                    if (!program.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host artifact Program.cs is missing required implementation token {token}.");
                }

                var requiredServiceTokens = new[]
                {
                "AiHostRuntimeOptions",
                "NativeModelFileProcessRunner",
                "NativeRunnerExecutable",
                "No upstream proxy fallback is used",
                "ProcessStartInfo"
            };

                foreach (var token in requiredServiceTokens)
                {
                    if (!architectureService.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host architecture service is missing required implementation token {token}.");
                }

                var requiredSettingTokens = new[]
                {
                "\"DefaultModel\"",
                "\"NativeRunnerExecutable\"",
                "\"ModelSearchRoots\"",
                "\"ContextTokens\"",
                "\"GpuLayers\""
            };

                foreach (var token in requiredSettingTokens)
                {
                    if (!appSettings.Contains(token, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host appsettings.json is missing required setting {token}.");
                }

                var requiredNavigationRoutes = new[]
                {
                "/chat",
                "/models",
                "/api-console",
                "/downloads",
                "/settings"
            };

                foreach (var route in requiredNavigationRoutes)
                {
                    if (!navigation.Contains(route, StringComparison.Ordinal))
                        throw new InvalidOperationException($"AI host navigation is missing required route {route}.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateAiHostArtifactContract solutionRoot:{solutionRoot.ToString()} projectName:{projectName.ToString()}");
             
            }
        }

        public static void ValidateGenerationContractJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                var requiredProperties = new[]
                {
                "schema",
                "project_kind",
                "target_platform",
                "complexity",
                "needs_datagen",
                "needs_tests",
                "needs_native_commands",
                "needs_index",
                "needs_version_resolver",
                "expected_entrypoints",
                "generated_files",
                "validation_status",
                "build_test_result_provenance"
            };

                foreach (var property in requiredProperties)
                    RequireJsonProperty(root, property, path, logger);

                RequireNonEmptyJsonArray(root, "expected_entrypoints", path, logger);
                RequireNonEmptyJsonArray(root, "generated_files", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationContractJson path:{path.ToString()}");

            }
           
        }

        public static void ValidateGenerationManifestJson(string path, ILogger logger)
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                RequireJsonProperty(root, "artifactKind", path, logger);
                RequireJsonProperty(root, "sourceGoal", path, logger);
                RequireJsonProperty(root, "validationStatus", path, logger);
                RequireJsonProperty(root, "buildTestResultProvenance", path, logger);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in ValidateGenerationManifestJson path:{path.ToString()}");

            }
       
        }

        public static void RequireJsonProperty(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                if (!root.TryGetProperty(propertyName, out _))
                    throw new InvalidOperationException($"Generated contract {Path.GetFileName(path)} is missing {propertyName}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireJsonProperty root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
            
        }

        public static void RequireNonEmptyJsonArray(JsonElement root, string propertyName, string path, ILogger logger)
        {
            try
            {
                RequireJsonProperty(root, propertyName, path, logger);
                var property = root.GetProperty(propertyName);
                if (property.ValueKind != JsonValueKind.Array || property.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException(
                        $"Generated contract {Path.GetFileName(path)} must include a non-empty {propertyName} array.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error in RequireNonEmptyJsonArray root:{root.ToString()} propertyName:{propertyName.ToString()} path:{path.ToString()}");

            }
        }

    }
}
