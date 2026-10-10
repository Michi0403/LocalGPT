using LocalGPT.BusinessObjects;
using LocalGPT.Interfaces;
using Microsoft.AspNetCore.Components;
using DevExpress.Blazor;

namespace LocalGPT.Components.Shared;

/// <summary>
/// Shared, database-backed approval, feedback and poll response editor used by Chat and Human Collaboration.
/// Both surfaces use the same request ID, version check, permission context and decision policy.
/// </summary>
public partial class HumanCollaborationRequestCard
{
    /// <summary>Persistent request shown in either review surface.</summary>
    [Parameter, EditorRequired]
    public HumanCollaborationRequest Request { get; set; } = default!;

    /// <summary>Reports a completed or stale decision so a parent may refresh its presentation.</summary>
    [Parameter]
    public EventCallback DecisionSaved { get; set; }

    /// <summary>Local per-card response draft.</summary>
    private string response = string.Empty;
    /// <summary>Local per-card human decision reason.</summary>
    private string reason = string.Empty;
    /// <summary>Selected user-authorized reuse scope.</summary>
    private HumanApprovalReuseScope reuseScope = HumanApprovalReuseScope.ExactRequestOnce;
    /// <summary>Controls whether a matching approval is single-use.</summary>
    private bool consumeApproval = true;
    /// <summary>True while the decision is being persisted.</summary>
    private bool isSubmitting;
    /// <summary>Visible result of a decision or a stale-card conflict.</summary>
    private string statusText = string.Empty;
    /// <summary>Last source request identity loaded into this editor.</summary>
    private Guid editorRequestId;
    /// <summary>The database decision version represented by this form.</summary>
    private int editorDecisionVersion;
    /// <summary>True for a security-sensitive approval instead of a nonapproval response.</summary>
    private bool IsApproval => string.Equals(Request.RequestKind, Vocabulary.Get().HumanRequestApproval, StringComparison.Ordinal);
    /// <summary>Read-only caption for the common DevExpress response editor.</summary>
    private string ResponseCaption => string.IsNullOrWhiteSpace(Request.ResponsePrompt) ? "Your answer or guidance" : Request.ResponsePrompt;
    /// <summary>Identifies an exact request without displaying its full fingerprint in the review card.</summary>
    private string FingerprintPreview => Request.ParameterFingerprint.Length <= 12
        ? Request.ParameterFingerprint : Request.ParameterFingerprint[..12];

    /// <summary>Available persisted exact-request approval scopes.</summary>
    private IReadOnlyList<LocalGptSelectionOption<HumanApprovalReuseScope>> ReuseScopes { get; } =
    [
        new(HumanApprovalReuseScope.ExactRequestOnce, "Exact request once"),
        new(HumanApprovalReuseScope.CurrentApplicationSession, "Current application session"),
        new(HumanApprovalReuseScope.PersistentUntilChanged, "Persistent until changed")
    ];

    /// <summary>Initializes the editor only when its database request identity or version changes.</summary>
    protected override void OnParametersSet()
    {
        try
        {
            if (Request.Id == editorRequestId && Request.DecisionVersion == editorDecisionVersion)
                return;
            editorRequestId = Request.Id;
            editorDecisionVersion = Request.DecisionVersion;
            response = Request.PrefillText;
            reason = Request.DecisionReason;
            reuseScope = Request.ApprovalReuseScope;
            consumeApproval = Request.ConsumeApproval;
            statusText = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize the shared human review card for request {RequestId}.", Request.Id);
            throw;
        }
    }

    /// <summary>Assigns a suggested answer without submitting or granting approval.</summary>
    /// <param name="suggestion">Suggested user-selected response.</param>
    private void ChooseSuggestion(string suggestion)
    {
        try
        {
            response = suggestion;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not select a suggested answer for request {RequestId}.", Request.Id);
            throw;
        }
    }

    /// <summary>Persists exactly one human decision through the application's canonical approval store.</summary>
    /// <param name="approved">Explicit approval or decline; null for feedback, guidance and polls.</param>
    /// <returns>Task completing when the decision is committed or recognized as stale.</returns>
    private async Task SubmitAsync(bool? approved)
    {
        if (isSubmitting)
            return;
        isSubmitting = true;
        try
        {
            var profile = await Collaboration.GetProfileAsync().ConfigureAwait(false);
            using var scope = HumanAmbientContext.PushHumanInteraction(
                RuntimePolicy.GetGuid(LocalGptRuntimeValue.LocalHumanProfileId),
                string.IsNullOrWhiteSpace(profile.DisplayName) ? "Human User" : profile.DisplayName,
                nameof(HumanCollaborationRequestCard),
                Request.CorrelationId,
                Request.CouncilRunId,
                Request.RequestedCouncilRound,
                Request.RequestedCouncilPhase);

            var resolved = await Collaboration.ResolveRequestAsync(
                Request.Id,
                new HumanDecisionSubmission(approved, response, reason, reuseScope, consumeApproval,
                    ExpectedDecisionVersion: editorDecisionVersion, RequirePending: true)).ConfigureAwait(false);
            statusText = resolved is null
                ? "This request was already answered in the other view. Its saved decision was not overwritten."
                : $"Decision saved as {resolved.Status} in Human Collaboration and Chat.";
            if (approved == true && resolved is not null)
            {
                var requestId = resolved.Id;
                TaskRunner.Run(nameof(HumanCollaborationRequestCard), "ExecuteApprovedHumanReview",
                    async cancellationToken =>
                    {
                        var results = await DeferredDxInvocations.ExecuteApprovedForApprovalRequestAsync(
                            requestId, cancellationToken: cancellationToken).ConfigureAwait(false);
                        Logger.LogInformation("Executed {Count} approved deferred function(s) after human request {RequestId}.", results.Count, requestId);
                    }, CancellationToken.None);
            }
            if (DecisionSaved.HasDelegate)
                await DecisionSaved.InvokeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Could not save shared human decision for request {RequestId}.", Request.Id);
            statusText = "Unable to save the decision. Review LocalGPT diagnostics.";
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
