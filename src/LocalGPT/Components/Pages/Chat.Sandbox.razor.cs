using LocalGPT.BusinessObjects;
using LocalGPT.Components.Shared;

namespace LocalGPT.Components.Pages;

public partial class Chat
{
    /// <summary>Temporary identifier for an unsaved Chat execution session.</summary>
    private readonly Guid scratchSandboxSessionId = Guid.NewGuid();
    /// <summary>Selected backend session for sandbox inspection and execution.</summary>
    private string selectedSandboxSession = string.Empty;
    /// <summary>Selected explicitly supported isolated guest runtime.</summary>
    private string selectedSandboxLanguage = "hyperlight-python";
    /// <summary>Editable source to review before launching a guest.</summary>
    private string sandboxSource = string.Empty;
    /// <summary>Visible feedback for the current generation or execution operation.</summary>
    private string sandboxFeedback = string.Empty;
    /// <summary>Namespace of the staged CodeDOM test type.</summary>
    private string sandboxCodeDomNamespace = "LocalGPT.Generated";
    /// <summary>Class name of the staged CodeDOM test type.</summary>
    private string sandboxCodeDomTypeName = "SandboxExample";
    /// <summary>Method name of the staged CodeDOM test type.</summary>
    private string sandboxCodeDomMethodName = "Describe";
    /// <summary>String returned by the CodeDOM test method.</summary>
    private string sandboxCodeDomMethodResult = "Generated with CodeDOM, executed only after user approval.";
    /// <summary>Marks code initially staged from the reviewed CodeDOM generator.</summary>
    private bool sandboxCodeDomDerived;

    /// <summary>Stages a runnable C# CodeDOM test for human review; this does not start a sandbox.</summary>
    private Task StageCodeDomSandboxSourceAsync()
    {
        try
        {
            sandboxSource = CodeDomSources.GenerateCSharpSandboxProgram(new CodeDomTypeSpec
            {
                Namespace = sandboxCodeDomNamespace, TypeName = sandboxCodeDomTypeName,
                MethodName = sandboxCodeDomMethodName, MethodResult = sandboxCodeDomMethodResult,
                Summary = "Sandbox test from the shared reviewed CodeDOM emitter."
            });
            sandboxCodeDomDerived = true;
            selectedSandboxLanguage = "hyperlight-csharp";
            sandboxFeedback = "CodeDOM source staged. Inspect or edit it below and explicitly select Run inside sandbox to execute it.";
        }
        catch (Exception exception)
        {
            sandboxFeedback = "CodeDOM source could not be generated: " + exception.Message;
            Logger.LogWarning(exception, "The requested CodeDOM sandbox preview could not be generated.");
        }
        return Task.CompletedTask;
    }

    private Guid CurrentSandboxSessionId => SessionContext.ConversationId
        ?? ActiveLiveCouncilSession?.RunId
        ?? scratchSandboxSessionId;

    private Guid EffectiveSandboxSessionId =>
        Guid.TryParse(selectedSandboxSession, out var sessionId) && sessionId != Guid.Empty
            ? sessionId : CurrentSandboxSessionId;

    private IReadOnlyList<SessionSandboxLanguage> SandboxLanguageOptions => SandboxRuns.Languages;
    private IReadOnlyList<SessionSandboxRunSnapshot> SessionSandboxRuns => SandboxRuns.ListRuns(EffectiveSandboxSessionId);
    private int TotalRunningSandboxes => SandboxRuns.ListRuns().Count(run => run.Status is "Queued" or "Running");

    private IReadOnlyList<LocalGptSelectionOption<string>> SandboxSessionOptions
    {
        get
        {
            var ids = SandboxRuns.ListRuns().Select(run => run.SessionId).ToList();
            ids.Add(CurrentSandboxSessionId);
            if (SessionContext.ConversationId is Guid conversationId) ids.Add(conversationId);
            if (ActiveLiveCouncilSession is { } councilSession) ids.Add(councilSession.RunId);
            return ids.Distinct().Select(id => new LocalGptSelectionOption<string>(id.ToString("D"),
                SessionContext.ConversationId == id ? "Current saved conversation" :
                ActiveLiveCouncilSession?.RunId == id ? "Active Council run" :
                id == CurrentSandboxSessionId ? "Current unsaved Chat session" :
                $"Previous session {id.ToString("N")[..8]}"))
                .ToArray();
        }
    }

    private Task StartSandboxExecutionAsync()
    {
        try
        {
            var request = new SessionSandboxRunRequest(
                EffectiveSandboxSessionId,
                SessionContext.ConversationId,
                SessionContext.ProjectId,
                selectedSandboxLanguage,
                sandboxSource,
                sandboxCodeDomDerived && selectedSandboxLanguage == "hyperlight-csharp" ? "CodeDOM-derived" : "Manual",
                UserConfirmed: true);
            var run = SandboxRuns.Start(request);
            sandboxFeedback = $"Isolated {run.Runtime} run queued for session {run.SessionId.ToString("N")[..8]}. Closing Chat will not stop it.";
        }
        catch (Exception exception)
        {
            sandboxFeedback = exception.Message;
            Logger.LogWarning(exception, "The explicitly requested isolated execution could not be queued.");
        }
        return Task.CompletedTask;
    }

    private Task StopSandboxExecutionAsync(Guid runId)
    {
        sandboxFeedback = SandboxRuns.Stop(runId)
            ? "Stop requested. The execution provider will terminate its micro-VM process tree."
            : "That sandbox already finished or is no longer available.";
        return Task.CompletedTask;
    }

    private Task RefreshSandboxExecutionAsync() => InvokeAsync(StateHasChanged);

    private void OnSandboxExecutionChanged()
    {
        if (!isDisposed && interactiveAttached)
            _ = InvokeAsync(StateHasChanged);
    }
}
