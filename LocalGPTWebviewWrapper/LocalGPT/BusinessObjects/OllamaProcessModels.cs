namespace LocalGPT.BusinessObjects;

public sealed record OllamaProcessInfo(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath);

public sealed record OllamaProcessStatus(
    bool IsInstalled,
    bool IsRunning,
    string? ExecutablePath,
    IReadOnlyList<OllamaProcessInfo> Processes,
    string Message);
