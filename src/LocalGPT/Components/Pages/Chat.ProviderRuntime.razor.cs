using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using static Microsoft.AspNetCore.Components.Web.RenderMode;
using LocalGPT;
using LocalGPT.Components;
using LocalGPT.Components.Layout;
using LocalGPT.BusinessObjects;
using LocalGPT.Services;
using DevExpress.Blazor;
using DevExpress.Blazor.Office;
using DevExpress.Blazor.RichEdit;
using DevExpress.Blazor.PivotTable;
using DevExpress.Blazor.PdfViewer;
using DevExpress.Blazor.Reporting.Models;
using LocalGPT.Interfaces;
using Microsoft.Extensions.Options;
using DevExpress.AIIntegration.Blazor.Chat;
using Microsoft.Extensions.AI;
using Markdig;
using System.Dynamic;
using System.Globalization;
using LocalGPT.Components.Shared;
using Microsoft.AspNetCore.Components;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalGPT.Components.Pages
{
    public partial class Chat
    {
    private void RebuildDynamicSessions()
    {
        try
        {
            if (ChatClientProvider is null)
                return;

            var previousSelection = ChatClientProvider.SelectedSession?.Name;
            var previousMessages = ChatClientProvider.AvailableChatClients
                .Where(filter => CouncilRuntime.IsDynamicSession(filter, Logger) ?? new())
                .ToDictionary(session => session.Name, session => session.Messages.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var session in ChatClientProvider.AvailableChatClients.Where(filter => CouncilRuntime.IsDynamicSession(filter, Logger) ?? new()).ToList())
            {
                ChatClientProvider.AvailableChatClients.Remove(session);
                session.Client.Dispose();
            }

            foreach (var candidate in OllamaCandidates.Where(candidate => candidate.IsInstalled))
            {
                var needsOllamaRuntimeOverride = candidate.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                    && ShouldAddRuntimeProfileSession();
                if (HasConfiguredSessionForModel(candidate) && !needsOllamaRuntimeOverride)
                    continue;

                var reference = candidate.ToReference();
                var client = ProviderModels.CreateChatClient(
                    reference,
                    keepAlive: "0s",
                    maxContextTokens: ResolveCouncilContextTokens(),
                    timeout: TimeSpan.FromMinutes(30),
                    ollamaNumGpu: candidate.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase)
                        ? ResolveOllamaNumGpu()
                        : null);
                var session = new ChatClientSession(
                    client,
                    CouncilRuntime.BuildDynamicSessionName(candidate, Logger),
                    candidate.Provider,
                    candidate.ModelName,
                    candidate.Endpoint);

                if (previousMessages.TryGetValue(session.Name, out var messages))
                    session.Messages.AddRange(messages);

                ChatClientProvider.AvailableChatClients.Add(session);
            }

            EnsureDiagnosticOllamaSession(previousMessages);

            var councilSession = new ChatClientSession(
                new CouncilChatClient(ServiceScopeFactory, CreateCouncilRequest, Logger, CouncilRuntime, CouncilText, Catalog, CouncilLiveSessions, ResolveCouncilDownloadUrl),
                Catalog.CouncilSessionName, "LocalGPT", "AI Council", Program.BaseUrl);
            if (previousMessages.TryGetValue(councilSession.Name, out var councilMessages))
                councilSession.Messages.AddRange(councilMessages);
            ChatClientProvider.AvailableChatClients.Add(councilSession);

            var preservedSelection = ChatClientProvider.AvailableChatClients
                .FirstOrDefault(session => session.Name.Equals(previousSelection, StringComparison.OrdinalIgnoreCase));
            var explicitlyRequestedDiagnostic = !string.IsNullOrWhiteSpace(DiagnosticRequestedSessionName);
            var configuredOllamaUnavailable = !explicitlyRequestedDiagnostic && IsUnavailableConfiguredOllamaSession(preservedSelection);

            var directCouncilSession = AutoStartCouncilStarter || !string.IsNullOrWhiteSpace(RequestedCouncilStarterKey)
                ? councilSession
                : null;
            ChatClientProvider.SelectedSession = directCouncilSession
                ?? (configuredOllamaUnavailable
                    ? SelectReachableFallbackSession()
                    : preservedSelection
                        ?? SelectReachableFallbackSession());

            if (configuredOllamaUnavailable && ChatClientProvider.SelectedSession is not null)
            {
                modelSelectionNotice = $"The previously selected Ollama model is configured but not reachable. LocalGPT selected {ChatClientProvider.SelectedSession.Name} instead.";
                Logger.LogWarning(
                    "Configured Ollama session {PreviousSession} was unavailable; selected fallback session {FallbackSession}.",
                    previousSelection,
                    ChatClientProvider.SelectedSession.Name);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }


    private bool IsUnavailableConfiguredOllamaSession(ChatClientSession? session)
    {
        try
        {
            if (session is null || !CouncilText.ContainsText(session.Provider, "Ollama"))
                return false;

            return !OllamaCandidates.Any(candidate =>
                candidate.IsInstalled &&
                candidate.SelectionKey.Equals(session.SelectionKey, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not determine whether the configured Ollama session is reachable.");
            return false;
        }
    }

    private ChatClientSession? SelectReachableFallbackSession()
    {
        try
        {
            if (ChatClientProvider is null)
                return null;

            return ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
                       CouncilText.StartsWithText(session.Name, Catalog.DetectedOllamaSessionPrefix))
                   ?? ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
                       !CouncilText.StartsWithText(session.Name, "Ollama — ") &&
                       !session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase))
                   ?? ChatClientProvider.AvailableChatClients.FirstOrDefault(session =>
                       session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase))
                   ?? ChatClientProvider.AvailableChatClients.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not select a reachable fallback AI session.");
            return ChatClientProvider?.AvailableChatClients.FirstOrDefault();
        }
    }

    private void EnsureDiagnosticOllamaSession(IReadOnlyDictionary<string, List<BlazorChatMessage>> previousMessages)
    {
        try
        {
            if (ChatClientProvider is null ||
            string.IsNullOrWhiteSpace(DiagnosticRequestedSessionName) ||
            DiagnosticRequestedSessionName.Equals("council", StringComparison.OrdinalIgnoreCase) ||
            FindRequestedDiagnosticSession(DiagnosticRequestedSessionName) is not null)
            {
                return;
            }

            var endpoint = DiagnosticOllamaEndpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
                endpoint = OllamaCandidates.FirstOrDefault()?.Endpoint;
            if (string.IsNullOrWhiteSpace(endpoint))
                endpoint = OllamaEndpoint;

            var session = new ChatClientSession(
                new OllamaThinkingChatClient(
                    new OllamaCoreOptions { Uri = endpoint, ModelName = DiagnosticRequestedSessionName },
                    Logger,
                    CouncilRuntime,
                    keepAlive: "0s",
                        contextLength: ResolveCouncilContextTokens(),
                        timeout: TimeSpan.FromMinutes(30),
                        numGpu: ResolveOllamaNumGpu(),
                        formatterFactory: ChatResponseFormatterFactory,
                        protocolResolver: ChatProtocolResolver,
                        promptConfigService: PromptConfigService,
                        functionRegistry: DxAiFunctionRegistry),
                $"{Catalog.DetectedOllamaSessionPrefix}{DiagnosticRequestedSessionName} @ {CouncilText.TrimEndpoint(endpoint, Logger)}", "Ollama", DiagnosticRequestedSessionName, endpoint);

            if (previousMessages.TryGetValue(session.Name, out var messages))
                session.Messages.AddRange(messages);

            ChatClientProvider.AvailableChatClients.Add(session);
            modelStatus = $"Diagnostic Ollama session created for {DiagnosticRequestedSessionName} at {endpoint}.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private MultiModelCouncilRequest CreateCouncilRequest()
    {
        try
        {
            SavePreparationConfiguration();
            var selectedModels = DiagnosticCouncilModelNames.Count > 0
            ? DiagnosticCouncilModelNames
            : SelectedCouncilModelNames;
            var selectedProviderModels = OllamaCandidates
                .Where(candidate => selectedModels.Contains(candidate.SelectionKey, StringComparer.OrdinalIgnoreCase))
                .Select(candidate => candidate.ToReference())
                .ToList();
            var selectedEndpoint = selectedProviderModels
                .FirstOrDefault(model => model.ProviderKind.Equals(ProviderModelKinds.Ollama, StringComparison.OrdinalIgnoreCase))
                ?.Endpoint;

            var userConfirmedArtifactBuild = GenerateCouncilArtifacts;
            GenerateCouncilArtifacts = false;

            var request = new MultiModelCouncilRequest
            {
                ModelNames = (CouncilRuntime.OrderCouncilModelsForLoad(selectedModels, Logger) ?? new List<string>()).ToList(),
                ModelSelections = selectedProviderModels,
                UnavailableModelSelections = (DiagnosticCouncilModelNames.Count > 0
                    ? DiagnosticCouncilModelNames
                        .Where(value => new ProviderModelIdentity().LooksProviderQualified(value))
                        .Where(value => !selectedProviderModels.Any(model => model.SelectionKey.Equals(value, StringComparison.OrdinalIgnoreCase)))
                        .Where(value => !IsSelectionEndpointStillConfigured(value))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList()
                    : BlockingUnavailableCouncilSelections.ToList()),
                BaseUri = selectedEndpoint,
                MaxRounds = Math.Clamp(CouncilCritiqueRounds, 0, 3),
                MaxOutputTokens = ResolveCouncilOutputTokens(),
                MaxParallelModels = Math.Max(1, CouncilMaxParallelModels),
                AllowParallelHardwareRoads = CouncilAllowParallelHardwareRoads,
                ResourceLoadPercent = Math.Clamp((int)Math.Round(CouncilResourceLoadPercent / 5d) * 5, 0, 100),
                ModelRoutes = CreateProviderQualifiedCouncilRoutes(),
                MaxContextTokens = ResolveCouncilContextTokens(),
                ModelTimeoutSeconds = Math.Clamp(CouncilModelTimeoutSeconds, 30, 1800),
                OllamaKeepAlive = "0s",
                OllamaNumGpu = ResolveOllamaNumGpu(),
                IncludeMemory = IncludeCouncilMemory,
                SaveToMemory = true,
                GenerateImplementationArtifact = userConfirmedArtifactBuild,
                UserConfirmedArtifactBuild = userConfirmedArtifactBuild,
                Title = $"DXAiChat AI Council · {CouncilTeams.FirstOrDefault(team => team.Key == SelectedCouncilTeamKey)?.DisplayName ?? SelectedCouncilTeamKey}",
                UseOrganicCouncilWorkflow = true,
                CouncilTeamKey = string.IsNullOrWhiteSpace(SelectedCouncilTeamKey) ? "general" : SelectedCouncilTeamKey,
                ProjectId = SessionContext.ProjectId,
                CreateProjectForRun = CreateProjectPerCouncilRun
            };

            var runtimeConfiguration = CouncilRunConfigurations.Ensure(request, request.ModelNames);
            ApplyCouncilRunConfigurationSnapshot(runtimeConfiguration);
            AttachedLiveCouncilRunId = request.RunId;
            RejoinCouncilRunId = request.RunId;
            SelectedCouncilRunId = request.RunId;
            ownsLiveCouncilStream = true;
            return request;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "Exception");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return new();
        }

    }

    private List<OneWireCouncilModelRoute> ParseModelRoutes(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<OneWireCouncilModelRoute>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private string ResolveCouncilDownloadUrl(string downloadUrl)
    {
        try
        {
            return NavigationManager.ToAbsoluteUri(downloadUrl).ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return downloadUrl;
        }
    }

    private void ApplyDiagnosticQueryOptions(bool selectSession)
    {
        try
        {
            if (ChatClientProvider is null)
                return;

            var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
            if (query.TryGetValue("diagCouncilMaxOutputTokens", out var maxTokens) &&
                int.TryParse(maxTokens.ToString(), out var parsedMaxTokens))
            {
                CouncilMaxOutputTokens = Math.Clamp(parsedMaxTokens, Catalog.MinCouncilOutputTokens, Catalog.MaxCouncilOutputTokens);
            }

            if (query.TryGetValue("diagCouncilMaxContextTokens", out var maxContext) &&
                int.TryParse(maxContext.ToString(), out var parsedMaxContext))
            {
                CouncilMaxContextTokens = Math.Clamp(parsedMaxContext, Catalog.MinCouncilContextTokens, Catalog.MaxCouncilContextTokens);
            }

            if (query.TryGetValue("diagMaxOutputTokens", out var maxOutputTokens) &&
                int.TryParse(maxOutputTokens.ToString(), out var parsedMaxOutputTokens))
            {
                ChatClientProvider.ForcedMaxOutputTokens = Math.Clamp(parsedMaxOutputTokens, 64, 262144);
            }

            if (query.TryGetValue("diagMaxPromptCharacters", out var maxPromptCharacters) &&
                int.TryParse(maxPromptCharacters.ToString(), out var parsedMaxPromptCharacters))
            {
                ChatClientProvider.ForcedMaxPromptCharacters = Math.Clamp(parsedMaxPromptCharacters, 512, 1_000_000);
            }

            if (query.TryGetValue("diagSkipBootstrap", out var skipBootstrap) &&
                bool.TryParse(skipBootstrap.ToString(), out var parsedSkipBootstrap))
            {
                ChatClientProvider.SuppressBootstrapContext = parsedSkipBootstrap;
            }

            if (query.TryGetValue("diagOllamaMode", out var ollamaMode))
            {
                var mode = ollamaMode.ToString();
                if (CouncilRuntime.TryIsSupportedOllamaMode(mode, Logger) ?? false)
                    OllamaAccelerationMode = mode;
            }

            if (query.TryGetValue("diagGpuLayers", out var gpuLayers) &&
                int.TryParse(gpuLayers.ToString(), out var parsedGpuLayers))
            {
                LimitedGpuLayers = Math.Clamp(parsedGpuLayers, 1, 99);
            }

            if (query.TryGetValue("diagCpuOnly", out var cpuOnly) &&
                bool.TryParse(cpuOnly.ToString(), out var parsedCpuOnly))
            {
                OllamaAccelerationMode = parsedCpuOnly ? Catalog.OllamaModeSafeCpu : Catalog.OllamaModeAutoGpu;
            }

            if (query.TryGetValue("diagCouncilIncludeMemory", out var includeMemory) &&
                bool.TryParse(includeMemory.ToString(), out var parsedIncludeMemory))
            {
                IncludeCouncilMemory = parsedIncludeMemory;
            }

            // Artifact generation is never enabled from a URL. The user must select it
            // explicitly in the current chat UI, and that confirmation is consumed once.
            GenerateCouncilArtifacts = false;

            if (query.TryGetValue("diagCouncilModels", out var councilModels))
            {
                DiagnosticCouncilModelNames = CouncilText.ParseModelNames(councilModels.ToString(), Logger).ToList();
                SelectedCouncilModelNames = DiagnosticCouncilModelNames.ToList();
            }

            if (query.TryGetValue("diagFreshChat", out var freshChat) &&
                bool.TryParse(freshChat.ToString(), out var parsedFreshChat))
            {
                UseFreshDiagnosticChat = parsedFreshChat;
                if (parsedFreshChat)
                    AutoLoadLatestConversation = false;
            }

            if (query.TryGetValue("diagSession", out var sessionValue))
                DiagnosticRequestedSessionName = sessionValue.ToString();

            if (query.TryGetValue("diagOllamaEndpoint", out var endpointValue))
            {
                var endpoint = endpointValue.ToString();
                if (!string.IsNullOrWhiteSpace(endpoint))
                    DiagnosticOllamaEndpoint = endpoint.TrimEnd('/');
            }

            if (!selectSession || string.IsNullOrWhiteSpace(DiagnosticRequestedSessionName))
                return;

            var requestedSession = DiagnosticRequestedSessionName;
            var selectedSession = FindRequestedDiagnosticSession(requestedSession);

            if (selectedSession is not null)
            {
                ChatClientProvider.SelectedSession = selectedSession;
                ChatClientProvider.LockedSessionName = selectedSession.Name;
                modelStatus = $"Diagnostic session locked to {selectedSession.Name}.";
            }

            if (DiagnosticCouncilModelNames.Count > 0)
                modelStatus = $"Diagnostic council model override: {CouncilText.FormatInlineNameList(DiagnosticCouncilModelNames)}. LocalGPT will not auto-select a different council member for this run.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    private ChatClientSession? FindRequestedDiagnosticSession(string requestedSession)
    {
        try
        {
            if (ChatClientProvider is null)
                return null;

            return requestedSession.Equals("council", StringComparison.OrdinalIgnoreCase)
                ? ChatClientProvider.AvailableChatClients.FirstOrDefault(session => session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase))
                : ChatClientProvider.AvailableChatClients.FirstOrDefault(session => (CouncilRuntime.IsDynamicSession(session, Logger) ?? false) && CouncilText.ContainsText(session.Name, requestedSession))
                    ?? ChatClientProvider.AvailableChatClients.FirstOrDefault(session => CouncilText.ContainsText(session.Name, requestedSession));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "Exception");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return null;
        }

    }

    private bool? MatchesRequestedDiagnosticSession(ChatClientSession session, string requestedSession)
    {
        try
        {
            if (requestedSession.Equals("council", StringComparison.OrdinalIgnoreCase))
                return session.Name.Equals(Catalog.CouncilSessionName, StringComparison.OrdinalIgnoreCase);

            return CouncilText.ContainsText(session.Name, requestedSession);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Operation {Operation} failed; request and generated payloads were omitted from logs.", "Exception");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return null;
        }
    }

    private bool ShouldAddRuntimeProfileSession()
    {
        try
        {
            if (!OllamaAccelerationMode.Equals(Catalog.OllamaModeAutoGpu, StringComparison.OrdinalIgnoreCase))
                return true;

            var query = QueryHelpers.ParseQuery(NavigationManager.ToAbsoluteUri(NavigationManager.Uri).Query);
            return query.ContainsKey("diagCpuOnly") || query.ContainsKey("diagOllamaMode");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return false;
        }
    }

    private void ClearSelectedSessionForFreshStart()
    {
        try
        {
            if (ChatClientProvider?.SelectedSession is null)
                return;

            ChatClientProvider.SelectedSession.Messages.Clear();
            PendingLiveCouncilUserMessages.Clear();
            ActiveConversationId = null;
            SessionContext.SetConversation(null);
            SelectedConversation = null;
            FeedbackTargets.Clear();
            SavedFeedback.Clear();
            SelectedFeedbackSortOrder = null;
            FeedbackComment = string.Empty;
            feedbackStatus = string.Empty;
            lastSavedSignature = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");

        }
    }

    private bool HasConfiguredSessionForModel(MultiModelCouncilModelCandidate candidate)
    {
        try
        {
            return ChatClientProvider?.AvailableChatClients.Any(session =>
                !(CouncilRuntime.IsDynamicSession(session, Logger) ?? false)
                && session.SelectionKey.Equals(candidate.SelectionKey, StringComparison.OrdinalIgnoreCase)) == true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Checking a provider-qualified configured session failed.");
            Notifier.ShowError(toastName, "The provider model session could not be checked. See local logs for details.", "Provider check failed");
            return false;
        }
    }





    private int? ResolveOllamaNumGpu()
    {
        try
        {
            if (string.Equals(
                OllamaAccelerationMode,
                Catalog.OllamaModeSafeCpu,
                StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (string.Equals(
                OllamaAccelerationMode,
                Catalog.OllamaModeLimitedGpu,
                StringComparison.OrdinalIgnoreCase))
            {
                return Math.Clamp(LimitedGpuLayers, 1, 99);
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return 0;
        }

    }

    private int ResolveCouncilOutputTokens()
    {
        try
        {
            return Math.Clamp(CouncilMaxOutputTokens, Catalog.MinCouncilOutputTokens, Catalog.MaxCouncilOutputTokens);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return 0;
        }
    }


    private int ResolveCouncilContextTokens()
    {
        try
        {
            return Math.Clamp(CouncilMaxContextTokens, Catalog.MinCouncilContextTokens, Catalog.MaxCouncilContextTokens);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
            return 0;
        }
    }



    private void OnOllamaAccelerationModeChanged(ChangeEventArgs e)
    {
        try
        {
            var requestedMode = Convert.ToString(e.Value) ?? Catalog.OllamaModeAutoGpu;
            if (!CouncilRuntime.TryIsSupportedOllamaMode(requestedMode, Logger) ?? false)
                requestedMode = Catalog.OllamaModeAutoGpu;

            OllamaAccelerationMode = requestedMode;
            if (DxAiChat is not null)
                SaveMessagesForSelectedSession(DxAiChat.SaveMessages());

            RebuildDynamicSessions();
            if (string.Equals(
                OllamaAccelerationMode,
                Catalog.OllamaModeSafeCpu,
                StringComparison.OrdinalIgnoreCase))
            {
                modelStatus = "Ollama acceleration: Safe CPU. LocalGPT sends num_gpu=0.";
            }
            else if (string.Equals(
                OllamaAccelerationMode,
                Catalog.OllamaModeLimitedGpu,
                StringComparison.OrdinalIgnoreCase))
            {
                modelStatus = $"Ollama acceleration: Limited GPU. LocalGPT sends num_gpu={Math.Clamp(LimitedGpuLayers, 1, 99)}.";
            }
            else
            {
                modelStatus = "Ollama acceleration: Auto GPU. LocalGPT lets Ollama choose GPU offload.";
            }

            SavePreparationConfiguration();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UI operation failed; user and model content were omitted from logs.");
            Notifier.ShowError(toastName, "The operation failed. See local application logs for technical details.", "Operation failed");
        }
    }

    }
}
