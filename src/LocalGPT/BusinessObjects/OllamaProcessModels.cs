namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an ollama process info.
/// </summary>
public sealed record OllamaProcessInfo(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath);

/// <summary>
/// Represents an ollama process status.
/// </summary>
public sealed record OllamaProcessStatus(
    bool IsInstalled,
    bool IsRunning,
    string? ExecutablePath,
    IReadOnlyList<OllamaProcessInfo> Processes,
    string ProcessSummary,
    string Message);
