using System.Diagnostics;
using LocalGPT.BusinessObjects;

namespace LocalGPT.Interfaces;

/// <summary>Pluggable, isolated execution provider. No provider may substitute host-side code execution.</summary>
public interface ISessionSandboxExecutionProvider
{
    /// <summary>Stable isolated-execution provider identifier.</summary>
    string Id { get; }
    /// <summary>Provides the language/runtime options contributed by the isolation provider.</summary>
    IReadOnlyList<SessionSandboxLanguage> Languages { get; }
    /// <summary>Creates a launch command that cannot run submitted code directly on the host.</summary>
    ProcessStartInfo PrepareLaunch(SessionSandboxLanguage language, string submittedSourcePath, string workingDirectory);
}

/// <summary>Persistent, process-owned execution registry keyed by Chat/Council session, not a Blazor circuit.</summary>
public interface ISessionSandboxExecutionService
{
    /// <summary>Notifies observers when backend execution receipts change.</summary>
    event Action? Changed;
    /// <summary>Provides the language/runtime options contributed by the isolation provider.</summary>
    IReadOnlyList<SessionSandboxLanguage> Languages { get; }
    /// <summary>Returns the backend-prepared guest provisioning status for the session UI.</summary>
    IReadOnlyList<SessionSandboxRuntimeReadiness> RuntimeReadiness { get; }
    /// <summary>Returns persisted receipts with optional session filtering.</summary>
    IReadOnlyList<SessionSandboxRunSnapshot> ListRuns(Guid? sessionId = null);
    /// <summary>Queues an explicitly submitted code run under its owning session.</summary>
    SessionSandboxRunSnapshot Start(SessionSandboxRunRequest request);
    /// <summary>Requests cancellation of the matching isolated guest.</summary>
    bool Stop(Guid runId);
}
