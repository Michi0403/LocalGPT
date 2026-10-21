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
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace LocalGPT.Components.Shared;

/// <summary>Preserves the existing console component interaction and state lifecycle in an independently reviewable partial declaration; reusable behavior remains delegated to its existing Services.</summary>
public partial class ChatGameConsole
{
    /// <summary>Starts a new corridor whose stable layout is derived from the supplied scenario text.</summary>
    /// <returns>A task that completes when the scenario-derived corridor has started.</returns>
    private async Task StartScenarioAsync()
    {
        try
        {
        var scenario = CouncilText.TrimForPrompt(scenarioPrompt, 240, Logger, collapseWhitespace: true);
        await StartAsync(
            "ascii-doom",
            snapshot?.TeamKey ?? "ascii-doom-council-adventure",
            scenario: scenario,
            campaignRuntimeClassKey: snapshot?.CampaignRuntimeClassKey ?? string.Empty,
            startingLevel: snapshot?.CurrentLevel,
            autoAdvanceLevels: snapshot?.AutoAdvanceLevels).ConfigureAwait(true);
        scenarioPrompt = string.Empty;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.StartScenarioAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change control mode for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="option">Selected DevExpress control-mode option.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeControlModeAsync(AsciiSelectOption<CouncilGameControlMode>? option)
    {
        try
        {
        if (snapshot is null || option is null) return;
        var mode = option.Value;
        snapshot = await Games.SetControlModeAsync(
            snapshot.Id,
            mode,
            mode == CouncilGameControlMode.Ai,
            snapshot.AutoplayDelayMilliseconds).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ChangeControlModeAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change autoplay delay for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="option">Selected DevExpress autoplay-delay option.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeAutoplayDelayAsync(AsciiSelectOption<int>? option)
    {
        try
        {
        if (snapshot is null || option is null) return;
        snapshot = await Games.SetControlModeAsync(
            snapshot.Id,
            snapshot.ControlMode,
            snapshot.ControlMode == CouncilGameControlMode.Ai,
            option.Value).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ChangeAutoplayDelayAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs toggle controls for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    private void ToggleControls()
    {
        try
        {
            showControls = !showControls;
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ToggleControls failed.");
            throw;
        }
    }

    /// <summary>Changes only the deterministic frame presentation; game mechanics, controls and session identity remain unchanged.</summary>
    /// <param name="option">Selected ASCII or pixel presentation.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeGameDisplayModeAsync(AsciiSelectOption<string>? option)
    {
        try
        {
        gameDisplayMode = string.Equals(option?.Value, "Pixel", StringComparison.OrdinalIgnoreCase) ? "Pixel" : "Ascii";
        lastJsLayoutSignature = string.Empty;
        await _gameConsoleModule!.InvokeVoidAsync("setDisplayMode", elementId, gameDisplayMode).ConfigureAwait(false);
        lastJsDisplayMode = gameDisplayMode;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ChangeGameDisplayModeAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs change fullscreen scale mode for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="option">Selected DevExpress fullscreen-scale option.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task ChangeFullscreenScaleModeAsync(AsciiSelectOption<string>? option)
    {
        try
        {
        var requestedMode = option?.Value;
        fullscreenScaleMode = string.Equals(requestedMode, "Native", StringComparison.OrdinalIgnoreCase)
            ? "Native"
            : string.Equals(requestedMode, "Width", StringComparison.OrdinalIgnoreCase) ? "Width" : "Fit";
        await _gameConsoleModule!.InvokeVoidAsync("setScaleMode", elementId, fullscreenScaleMode).ConfigureAwait(true);
        lastJsScaleMode = fullscreenScaleMode;
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.ChangeFullscreenScaleModeAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs fullscreen for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task FullscreenAsync()
    {
        try
        {
            await _gameConsoleModule!.InvokeVoidAsync("fullscreen", elementId).ConfigureAwait(true);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.FullscreenAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs close for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task CloseAsync()
    {
        try
        {
        await _gameConsoleModule!.InvokeVoidAsync("exitFullscreen", elementId).ConfigureAwait(true);
        if (CloseRequested.HasDelegate)
            await InvokeAsync(() => CloseRequested.InvokeAsync()).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.CloseAsync failed.");
            throw;
        }
    }

    /// <summary>Ends only the active ASCII game while preserving the chat, Council/provider sessions and reusable terminal surface.</summary>
    /// <returns>A task that completes after the authoritative game session has ended.</returns>
    private async Task EndGameAsync()
    {
        try
        {
        if (snapshot is null)
            return;
        snapshot = await Games.EndAsync(snapshot.Id, "Human User").ConfigureAwait(true);
        await ReportGameAvailabilityAsync().ConfigureAwait(true);
        conversationMode = true;
        operatorMode = false;
        await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.EndGameAsync failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs submit control from JavaScript for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="action">Action value supplied to the chat game console operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    [JSInvokable]
    public Task SubmitControlFromJs(string action)
    {
        try
        {
        if (HasHotSeatRequest)
        {
            var command = action switch
            {
                "move-forward" => "MOVE N",
                "move-backward" => "MOVE S",
                "strafe-left" or "turn-left" => "MOVE W",
                "strafe-right" or "turn-right" => "MOVE E",
                "shoot" => "DASH N",
                "use" => "PASS",
                _ => string.Empty
            };
            return string.IsNullOrWhiteSpace(command) ? Task.CompletedTask : SubmitHotSeatResponseAsync(command);
        }
        return SendControlAsync(action);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.SubmitControlFromJs failed.");
            throw;
        }
    }

    /// <summary>
    /// Performs send control for <see cref="ChatGameConsole"/>, keeping the operation consistent with the state and invariants of the surrounding chat game console workflow.
    /// </summary>
    /// <param name="action">Action value supplied to the chat game console operation and used when producing its result.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    private async Task SendControlAsync(string action)
    {
        if (snapshot is null || !CanSubmitHumanControl) return;
        var sessionId = snapshot.Id;
        var expectedTurn = snapshot.Turn;
        inputInFlight = true;
        await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        try
        {
            snapshot = await Games.ApplyControlAsync(new CouncilGameControlRequest
            {
                SessionId = sessionId,
                Action = action,
                ExpectedTurn = expectedTurn,
                Source = "HumanUI",
                ActorName = "Human Player"
            }).ConfigureAwait(true);
            await Task.Delay(260).ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "The in-chat game control {Action} was rejected.", action);
            snapshot = await Games.GetAsync(sessionId).ConfigureAwait(true);
        }
        finally
        {
            inputInFlight = false;
            await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

    /// <summary>Refreshes pending human requests projected directly into the active ASCII game surface.</summary>
    /// <returns>A task that completes after the current Council requests have been projected.</returns>
    private async Task RefreshHotSeatRequestAsync()
    {
        if (CouncilRunId is not Guid runId)
        {
            hotSeatRequest = null;
            gameInteractionRequest = null;
            return;
        }

        try
        {
            var collaboration = await HumanCollaboration.GetSnapshotAsync(includeResolved: false, take: 80).ConfigureAwait(true);
            hotSeatProfile = collaboration.Profile;
            var pending = collaboration.Requests
                .Where(request => request.CouncilRunId == runId && request.DecidedAtUtc is null)
                .OrderBy(request => request.RequestedAtUtc)
                .ToList();
            hotSeatRequest = pending.FirstOrDefault(AsciiText.IsCouncilRoleResponseRequest);
            gameInteractionRequest = pending.FirstOrDefault(request => !AsciiText.IsCouncilRoleResponseRequest(request));

            if ((hotSeatRequest is not null || gameInteractionRequest is not null) && !operatorMode && snapshot is not null)
                conversationMode = false;
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Refreshing in-game Human Collaboration requests failed.");
        }
    }

    /// <summary>Resolves the current Council HumanOnly player role directly from the game surface.</summary>
    /// <param name="command">Validated MOVE, DASH, PASS or EMOTE command submitted by the human player.</param>
    /// <returns>A task that completes when the Human Collaboration request has been resolved.</returns>
    private async Task SubmitHotSeatResponseAsync(string command)
    {
        var request = hotSeatRequest;
        if (request is null || hotSeatSubmitting || string.IsNullOrWhiteSpace(command))
            return;

        hotSeatSubmitting = true;
        try
        {
            var profile = hotSeatProfile ?? await HumanCollaboration.GetProfileAsync().ConfigureAwait(true);
            using var scope = HumanAmbientContext.PushHumanInteraction(
                profile.Id,
                profile.DisplayName,
                nameof(ChatGameConsole),
                request.CorrelationId,
                request.CouncilRunId,
                request.RequestedCouncilRound,
                request.RequestedCouncilPhase);
            await HumanCollaboration.ResolveRequestAsync(
                request.Id,
                new HumanDecisionSubmission(null, command.Trim(), string.Empty),
                consoleRenderCancellation.Token).ConfigureAwait(true);
            hotSeatText = string.Empty;
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (consoleRenderCancellation.IsCancellationRequested)
        {
            // Component teardown cancels a still-pending player submission.
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Submitting a Council hot-seat move from the ASCII game surface failed.");
        }
        finally
        {
            hotSeatSubmitting = false;
            if (!disposed)
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

    /// <summary>Submits the free-text field as an EMOTE command while keeping movement deterministic.</summary>
    /// <returns>A task that completes when the EMOTE response has been submitted.</returns>
    private Task SubmitHotSeatTextAsync()
    {
        try
        {
        var text = CouncilText.TrimForPrompt(hotSeatText, 80, Logger, collapseWhitespace: true);
        if (string.IsNullOrWhiteSpace(text))
            return Task.CompletedTask;
        return SubmitHotSeatResponseAsync($"EMOTE {text}");
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.SubmitHotSeatTextAsync failed.");
            throw;
        }
    }

    /// <summary>Submits one projected AI/Council request response without leaving the active ASCII game.</summary>
    /// <param name="approved">Optional approval decision for approval-style requests.</param>
    /// <param name="response">Human response text for free-text or suggested-response requests.</param>
    /// <param name="reason">Optional human decision reason recorded with approval responses.</param>
    /// <returns>A task that completes when the Human Collaboration request has been resolved.</returns>
    private async Task SubmitGameInteractionAsync(bool? approved, string response, string reason)
    {
        var request = gameInteractionRequest;
        if (request is null || gameInteractionSubmitting)
            return;

        var isApproval = string.Equals(request.RequestKind, Vocabulary.Get().HumanRequestApproval, StringComparison.Ordinal);
        var normalizedResponse = CouncilText.TrimForPrompt(response, 2000, Logger, collapseWhitespace: true);
        var normalizedReason = CouncilText.TrimForPrompt(reason, 2000, Logger, collapseWhitespace: true);
        if (!isApproval && string.IsNullOrWhiteSpace(normalizedResponse))
            return;
        if (isApproval && approved == false && string.IsNullOrWhiteSpace(normalizedReason))
            normalizedReason = "Declined from the active ASCII game surface.";

        gameInteractionSubmitting = true;
        try
        {
            var profile = hotSeatProfile ?? await HumanCollaboration.GetProfileAsync().ConfigureAwait(true);
            using var scope = HumanAmbientContext.PushHumanInteraction(
                profile.Id,
                profile.DisplayName,
                nameof(ChatGameConsole),
                request.CorrelationId,
                request.CouncilRunId,
                request.RequestedCouncilRound,
                request.RequestedCouncilPhase);
            var resolved = await HumanCollaboration.ResolveRequestAsync(
                request.Id,
                new HumanDecisionSubmission(approved, normalizedResponse, normalizedReason),
                consoleRenderCancellation.Token).ConfigureAwait(true);
            if (approved == true && resolved is not null)
                QueueApprovedGameInteractionExecution(request.Id);
            gameInteractionText = string.Empty;
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
        }
        catch (OperationCanceledException) when (consoleRenderCancellation.IsCancellationRequested)
        {
            // Component teardown cancels a still-pending in-game response.
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "Submitting an AI/Council request from the ASCII game surface failed.");
            operatorStatus = "The in-game Council request could not be saved. Review LocalGPT diagnostics.";
        }
        finally
        {
            gameInteractionSubmitting = false;
            if (!disposed)
                await InvokeAsync(StateHasChanged).ConfigureAwait(true);
        }
    }

    /// <summary>Queues deferred DXFunction calls approved from the ASCII surface without blocking terminal input.</summary>
    /// <param name="approvalRequestId">Approval request whose deferred function calls may execute.</param>
    private void QueueApprovedGameInteractionExecution(Guid approvalRequestId)
    {
        try
        {
        TaskRunner.Run(
            nameof(ChatGameConsole),
            "ExecuteApprovedAsciiDeferredCalls",
            async cancellationToken =>
            {
                var outcomes = await DeferredDxInvocations
                    .ExecuteApprovedForApprovalRequestAsync(approvalRequestId, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                if (outcomes.Count == 0 || disposed)
                    return;
                var failed = outcomes.Count(outcome => !string.Equals(
                    outcome.Status,
                    Vocabulary.Get().DeferredCompleted,
                    StringComparison.OrdinalIgnoreCase));
                await InvokeAsync(() =>
                {
                    operatorStatus = failed == 0
                        ? $"Approved AI action completed ({outcomes.Count} function call(s))."
                        : $"Approved AI action completed with {failed} failure(s) across {outcomes.Count} function call(s).";
                    StateHasChanged();
                }).ConfigureAwait(false);
            },
            consoleRenderCancellation.Token);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.QueueApprovedGameInteractionExecution failed.");
            throw;
        }
    }

    /// <summary>Submits the free-text response currently entered for the projected in-game AI/Council request.</summary>
    /// <returns>A task that completes when the current in-game response has been submitted.</returns>
    private Task SubmitGameInteractionTextAsync()
    {
        try
        {
            return SubmitGameInteractionAsync(null, gameInteractionText, string.Empty);
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.SubmitGameInteractionTextAsync failed.");
            throw;
        }
    }

    /// <summary>Reacts to Human Collaboration updates without polling the database from the renderer.</summary>
    private void OnHumanCollaborationChanged()
    {
        try
        {
        if (disposed)
            return;
        _ = InvokeAsync(async () =>
        {
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
            StateHasChanged();
        });
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.OnHumanCollaborationChanged failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the game changed lifecycle or event notification for <see cref="ChatGameConsole"/>, updating the state required by the surrounding workflow.
    /// </summary>
    /// <param name="sessionId">Identifier of the session to use for this operation.</param>
    private void OnGameChanged(Guid sessionId)
    {
        try
        {
        if (snapshot is not null && snapshot.Id != sessionId && CouncilRunId is null && ConversationId is null)
            return;
        _ = InvokeAsync(async () =>
        {
            var hadSnapshot = snapshot is not null;
            var candidate = await Games.GetAsync(sessionId).ConfigureAwait(true);
            if (candidate is null || !MatchesCurrentGameContext(candidate))
                return;
            if (string.Equals(SuggestedTeamKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(candidate.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            snapshot = candidate;
            await ReportGameAvailabilityAsync().ConfigureAwait(true);
            await RefreshHotSeatRequestAsync().ConfigureAwait(true);
            if (!hadSnapshot && !operatorMode)
                conversationMode = false;
            StateHasChanged();
        });
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.OnGameChanged failed.");
            throw;
        }
    }

    /// <summary>
    /// Handles the console changed lifecycle or event notification for <see cref="ChatGameConsole"/>, updating the state required by the surrounding workflow.
    /// </summary>
    private void OnConsoleChanged()
    {
        try
        {
        if (disposed)
            return;
        Interlocked.Exchange(ref consoleRenderPending, 1);
        if (Interlocked.CompareExchange(ref consoleRenderWorkerRunning, 1, 0) == 0)
            TaskRunner.Run(nameof(ChatGameConsole), nameof(RefreshConsoleRenderAsync), _ => RefreshConsoleRenderAsync(), consoleRenderCancellation.Token);
    
        }
        catch (Exception __componentMethodException)
        {
            Logger.LogError(__componentMethodException, "Component method ChatGameConsole.OnConsoleChanged failed.");
            throw;
        }
    }

    /// <summary>Coalesces high-frequency process progress into a bounded chat-console renderer cadence.</summary>
    /// <returns>A task that completes after the newest bounded console snapshot is rendered.</returns>
    private async Task RefreshConsoleRenderAsync()
    {
        try
        {
            while (!consoleRenderCancellation.IsCancellationRequested)
            {
                await Task.Delay(250, consoleRenderCancellation.Token).ConfigureAwait(false);
                Interlocked.Exchange(ref consoleRenderPending, 0);
                await InvokeAsync(() =>
                {
                    consoleOutput = Console.GetRecentOutput(160);
                    StateHasChanged();
                }).ConfigureAwait(false);
                if (Volatile.Read(ref consoleRenderPending) == 0)
                    break;
            }
        }
        catch (OperationCanceledException) when (consoleRenderCancellation.IsCancellationRequested)
        {
            // Component disposal cancels delayed progress rendering.
        }
        catch (ObjectDisposedException)
        {
            // Browser/circuit teardown can dispose the renderer while a delayed refresh is pending.
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "A coalesced LocalGPT console renderer refresh failed.");
        }
        finally
        {
            Interlocked.Exchange(ref consoleRenderWorkerRunning, 0);
            if (!disposed
                && Volatile.Read(ref consoleRenderPending) != 0
                && !consoleRenderCancellation.IsCancellationRequested
                && Interlocked.CompareExchange(ref consoleRenderWorkerRunning, 1, 0) == 0)
                TaskRunner.Run(nameof(ChatGameConsole), nameof(RefreshConsoleRenderAsync), _ => RefreshConsoleRenderAsync(), consoleRenderCancellation.Token);
        }
    }

    /// <summary>
    /// Releases resources owned by <see cref="ChatGameConsole"/> and leaves the chat game console workflow in a safely disposed state.
    /// </summary>
    /// <returns>A task that completes when the operation has finished.</returns>
    public async ValueTask DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        Games.Changed -= OnGameChanged;
        HumanCollaboration.Changed -= OnHumanCollaborationChanged;
        Console.Changed -= OnConsoleChanged;
        consoleRenderCancellation.Cancel();
        try
        {
            if (attached)
                await _gameConsoleModule!.InvokeVoidAsync("detach", elementId).ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            Logger.LogDebug("ASCII game console detach ended after browser disconnect.");
        }
        catch (OperationCanceledException)
        {
            Logger.LogDebug("ASCII game console detach was canceled during component teardown.");
        }
        catch (ObjectDisposedException)
        {
            Logger.LogDebug("ASCII game console browser resources were already disposed during component teardown.");
        }
        catch (Exception exception)
        {
            Logger.LogWarning(exception, "ASCII game console detach failed during component teardown.");
        }
        finally
        {
            attached = false;
            if (_gameConsoleModule is not null)
            {
                try
                {
                    await _gameConsoleModule.DisposeAsync().ConfigureAwait(false);
                }
                catch (JSDisconnectedException exception)
                {
                    Logger.LogDebug(exception, "Game console component module disconnected during disposal.");
                }
                catch (ObjectDisposedException exception)
                {
                    Logger.LogDebug(exception, "Game console component module was already disposed.");
                }
                catch (Exception exception)
                {
                    Logger.LogWarning(exception, "Game console component module could not be disposed.");
                }
                finally
                {
                    _gameConsoleModule = null;
                }
            }
            reference?.Dispose();
            reference = null;
            consoleRenderCancellation.Dispose();
            GC.SuppressFinalize(this);
        }
    }


    /// <summary>Computes HasHotSeatRequest for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasHotSeatRequestForRender()
    {
        try
        {
            return hotSeatRequest is not null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.HasHotSeatRequest failed.");
            throw;
        }
    }

    /// <summary>Computes HasGameInteractionRequest for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetHasGameInteractionRequestForRender()
    {
        try
        {
            return gameInteractionRequest is not null;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.HasGameInteractionRequest failed.");
            throw;
        }
    }

    /// <summary>Computes IsApprovalGameInteraction for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetIsApprovalGameInteractionForRender()
    {
        try
        {
            return gameInteractionRequest is not null
        && string.Equals(gameInteractionRequest.RequestKind, Vocabulary.Get().HumanRequestApproval, StringComparison.Ordinal);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.IsApprovalGameInteraction failed.");
            throw;
        }
    }

    /// <summary>Computes IsCouncilHotSeatGame for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetIsCouncilHotSeatGameForRender()
    {
        try
        {
            return CouncilRunId.HasValue
        && (HasHotSeatRequest
            || string.Equals(snapshot?.TeamKey, "ascii-hot-seat-showcase", StringComparison.OrdinalIgnoreCase)
            || string.Equals(SuggestedTeamKey, "ascii-hot-seat-showcase", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.IsCouncilHotSeatGame failed.");
            throw;
        }
    }

    /// <summary>Computes CanRestartCorridor for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanRestartCorridorForRender()
    {
        try
        {
            return snapshot is { GameKey: "ascii-doom" }
        && !string.Equals(snapshot.Status, "Running", StringComparison.OrdinalIgnoreCase)
        && !IsCouncilHotSeatGame;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.CanRestartCorridor failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedFullscreenScaleOption for the render path with component-local diagnostics.</summary>
    /// <returns>The ascii select option string produced by the operation.</returns>
    private AsciiSelectOption<string> GetSelectedFullscreenScaleOptionForRender()
    {
        try
        {
            return FullscreenScaleOptions.FirstOrDefault(option => string.Equals(option.Value, fullscreenScaleMode, StringComparison.OrdinalIgnoreCase))
        ?? FullscreenScaleOptions[0];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.SelectedFullscreenScaleOption failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedGameDisplayOption for the render path with component-local diagnostics.</summary>
    /// <returns>The ascii select option string produced by the operation.</returns>
    private AsciiSelectOption<string> GetSelectedGameDisplayOptionForRender()
    {
        try
        {
            return GameDisplayOptions.FirstOrDefault(option => string.Equals(option.Value, gameDisplayMode, StringComparison.OrdinalIgnoreCase))
        ?? GameDisplayOptions[0];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.SelectedGameDisplayOption failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedControlModeOption for the render path with component-local diagnostics.</summary>
    /// <returns>The ascii select option council game control mode produced by the operation.</returns>
    private AsciiSelectOption<CouncilGameControlMode> GetSelectedControlModeOptionForRender()
    {
        try
        {
            return ControlModeOptions.FirstOrDefault(option => option.Value == snapshot?.ControlMode)
        ?? ControlModeOptions[1];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.SelectedControlModeOption failed.");
            throw;
        }
    }

    /// <summary>Computes SelectedAutoplayDelayOption for the render path with component-local diagnostics.</summary>
    /// <returns>The ascii select option int produced by the operation.</returns>
    private AsciiSelectOption<int> GetSelectedAutoplayDelayOptionForRender()
    {
        try
        {
            return AutoplayDelayOptions.FirstOrDefault(option => option.Value == snapshot?.AutoplayDelayMilliseconds)
        ?? AutoplayDelayOptions[2];
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.SelectedAutoplayDelayOption failed.");
            throw;
        }
    }

    /// <summary>Computes CanShowControlGuide for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanShowControlGuideForRender()
    {
        try
        {
            return snapshot is not null
        && (HasHotSeatRequest
            || HasGameInteractionRequest
            || CanRestartCorridor
            || (snapshot.LegalActions.Count > 0 && snapshot.ControlMode != CouncilGameControlMode.Ai));
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.CanShowControlGuide failed.");
            throw;
        }
    }

    /// <summary>Computes ShowControlGuide for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetShowControlGuideForRender()
    {
        try
        {
            return showControls && CanShowControlGuide;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.ShowControlGuide failed.");
            throw;
        }
    }

    /// <summary>Computes IsKernelTournament for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetIsKernelTournamentForRender()
    {
        try
        {
            return string.Equals(snapshot?.GameKey, "kernel-creature-tournament", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.IsKernelTournament failed.");
            throw;
        }
    }

    /// <summary>Computes CanSubmitHumanControl for the render path with component-local diagnostics.</summary>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    private bool GetCanSubmitHumanControlForRender()
    {
        try
        {
            return HasHotSeatRequest
        ? !hotSeatSubmitting
        : snapshot is { HumanInputRequired: true, ControlMode: not CouncilGameControlMode.Ai } && !inputInFlight;
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.CanSubmitHumanControl failed.");
            throw;
        }
    }

    /// <summary>Computes InputBusyState for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetInputBusyStateForRender()
    {
        try
        {
            return inputInFlight || hotSeatSubmitting || gameInteractionSubmitting ? "true" : "false";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.InputBusyState failed.");
            throw;
        }
    }

    /// <summary>Computes ConsoleText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetConsoleTextForRender()
    {
        try
        {
            return Console.GetRecentDisplayText(160);
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.ConsoleText failed.");
            throw;
        }
    }

    /// <summary>Computes OperatorConsoleText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetOperatorConsoleTextForRender()
    {
        try
        {
            return consoleOutput.Count > 0 ? Console.GetRecentDisplayText(240) : "┌─ LOCALGPT MATRIX OPERATOR ───────────────────────────────────────────────────┐" + Environment.NewLine + "│ Embedded shell/process control ready. No external terminal window is opened. │" + Environment.NewLine + "│ :help lists app + shell commands; chat, Council and games stay in this wall.  │" + Environment.NewLine + "└──────────────────────────────────────────────────────────────────────────────┘";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.OperatorConsoleText failed.");
            throw;
        }
    }

    /// <summary>Computes OperatorHeaderText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetOperatorHeaderTextForRender()
    {
        try
        {
            return operatorMode
        ? $"Embedded {OperatorShellLabel} · {ActiveJobText} · game state preserved behind this wall"
        : snapshot is null ? (consoleOutput.Count > 0 ? "Live bounded LocalGPT terminal output. Operator mode can take interactive control." : "Start a directly playable runtime-class game or open the operator layer.") : $"Turn {snapshot.Turn} · {snapshot.CurrentTurnOwner} · {snapshot.ControlMode}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.OperatorHeaderText failed.");
            throw;
        }
    }

    /// <summary>Computes OperatorShellLabel for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetOperatorShellLabelForRender()
    {
        try
        {
            return operatorShell == LocalConsoleShellKind.PowerShell ? "pwsh" : operatorShell.ToString().ToLowerInvariant();
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.OperatorShellLabel failed.");
            throw;
        }
    }

    /// <summary>Computes OperatorBusyState for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetOperatorBusyStateForRender()
    {
        try
        {
            return operatorSubmitting ? "true" : "false";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.OperatorBusyState failed.");
            throw;
        }
    }

    /// <summary>Computes ActiveJobText for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetActiveJobTextForRender()
    {
        try
        {
            return $"{Console.GetActiveOperations().Count} active job(s)";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.ActiveJobText failed.");
            throw;
        }
    }

    /// <summary>Computes AsciiSurfaceTitle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetAsciiSurfaceTitleForRender()
    {
        try
        {
            return operatorMode
        ? "LOCALGPT // MATRIX OPERATOR"
        : conversationMode
            ? "LOCALGPT // ASCII CHAT"
            : snapshot?.DisplayName ?? "LOCALGPT // ASCII GAME";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.AsciiSurfaceTitle failed.");
            throw;
        }
    }

    /// <summary>Computes AsciiSurfaceSubtitle for the render path with component-local diagnostics.</summary>
    /// <returns>The string produced by the operation.</returns>
    private string GetAsciiSurfaceSubtitleForRender()
    {
        try
        {
            return operatorMode
        ? OperatorHeaderText
        : conversationMode
            ? $"{ConversationMessages.Count} shared chat message(s) · function/thinking traces included · {(FunModeEnabled ? "fun mode on" : "fun mode off")}"
            : snapshot is null
                ? "No game is running; CHAT returns to the replayable conversation."
                : $"Turn {snapshot.Turn} · {snapshot.CurrentTurnOwner} · {snapshot.ControlMode}";
        }
        catch (Exception __componentPropertyException)
        {
            Logger.LogError(__componentPropertyException, "Computed render property ChatGameConsole.AsciiSurfaceSubtitle failed.");
            throw;
        }
    }
}
