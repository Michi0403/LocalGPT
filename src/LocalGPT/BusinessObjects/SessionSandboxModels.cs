namespace LocalGPT.BusinessObjects;

/// <summary>Execution languages are advertised by installed isolation providers, not Council roles or seeded model lists.</summary>
/// <param name="Key">Stable provider/language key.</param>
/// <param name="ProviderId">Execution provider identifier.</param>
/// <param name="DisplayName">User-facing capability label.</param>
/// <param name="Runtime">Guest runtime image.</param>
/// <param name="SourceExtension">Temporary submitted source extension.</param>
public sealed record SessionSandboxLanguage(string Key, string ProviderId, string DisplayName, string Runtime, string SourceExtension);

/// <summary>Installation-time guest capability evidence, prepared by the backend rather than evaluated during Razor rendering.</summary>
public sealed record SessionSandboxRuntimeReadiness(string DisplayName, string Runtime, bool Provisioned, string Explanation);

/// <summary>A request explicitly approved by the human using the Chat session tools.</summary>
/// <param name="SessionId">Owning session ID.</param>
/// <param name="ConversationId">Optional saved conversation ID.</param>
/// <param name="ProjectId">Optional project ID.</param>
/// <param name="LanguageKey">Selected registered language.</param>
/// <param name="Source">Explicitly submitted source code.</param>
/// <param name="SourceKind">Origin of the submitted source, such as a reviewed CodeDOM preview.</param>
/// <param name="UserConfirmed">Fresh explicit user confirmation for this sandbox invocation.</param>
public sealed record SessionSandboxRunRequest(
    Guid SessionId,
    Guid? ConversationId,
    Guid? ProjectId,
    string LanguageKey,
    string Source,
    string SourceKind = "Manual",
    bool UserConfirmed = false);

/// <summary>Backend-owned, UI-independent execution evidence. Source text is intentionally not persisted.</summary>
public sealed record SessionSandboxRunSnapshot
{
    /// <summary>Identifies the isolated run.</summary>
    public required Guid RunId { get; init; }
    /// <summary>Identifies the owning session.</summary>
    public required Guid SessionId { get; init; }
    /// <summary>Identifies the owning conversation when available.</summary>
    public Guid? ConversationId { get; init; }
    /// <summary>Identifies the associated project when available.</summary>
    public Guid? ProjectId { get; init; }
    /// <summary>Names the isolation provider.</summary>
    public required string ProviderId { get; init; }
    /// <summary>Names the selected runtime capability.</summary>
    public required string LanguageKey { get; init; }
    /// <summary>Names the guest runtime image.</summary>
    public required string Runtime { get; init; }
    /// <summary>Reports the backend run status.</summary>
    public required string Status { get; init; }
    /// <summary>Timestamp when execution was queued.</summary>
    public required DateTimeOffset StartedUtc { get; init; }
    /// <summary>Timestamp when execution finished.</summary>
    public DateTimeOffset? CompletedUtc { get; init; }
    /// <summary>Exit code returned by the guest.</summary>
    public int? ExitCode { get; init; }
    /// <summary>Bounded captured stdout evidence.</summary>
    public string StandardOutput { get; init; } = string.Empty;
    /// <summary>Bounded captured stderr evidence.</summary>
    public string StandardError { get; init; } = string.Empty;
    /// <summary>Operational error or outcome.</summary>
    public string Diagnostic { get; init; } = string.Empty;
    /// <summary>Signals when output was truncated.</summary>
    public bool OutputTruncated { get; init; }
    /// <summary>SHA-256 of the submitted code, without persisting executable source.</summary>
    public string SourceSha256 { get; init; } = string.Empty;
    /// <summary>Source origin retained for reproducible debugging and review.</summary>
    public string SourceKind { get; init; } = "Manual";
    /// <summary>Describes the guest permissions.</summary>
    public string Permissions { get; init; } = "Guest network disabled; host filesystem mounts disabled; only submitted source is passed to the runtime.";
}
