namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents an Ollama process info application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="ProcessId">Identifier of the process to use for this operation.</param>
/// <param name="ProcessName">Process name value supplied to the Ollama process info operation and used when producing its result.</param>
/// <param name="ExecutablePath">Executable path value supplied to the Ollama process info operation and used when producing its result.</param>
public sealed record OllamaProcessInfo(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath);

/// <summary>
/// Represents an Ollama process status application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="IsInstalled">Value indicating whether installed should apply to this operation.</param>
/// <param name="IsRunning">Value indicating whether running should apply to this operation.</param>
/// <param name="ExecutablePath">Executable path value supplied to the Ollama process status operation and used when producing its result.</param>
/// <param name="Processes">Ollama process info dependency used by the Ollama process status workflow to provide the corresponding application capability.</param>
/// <param name="ProcessSummary">Process summary value supplied to the Ollama process status operation and used when producing its result.</param>
/// <param name="Message">Message value supplied to the Ollama process status operation and used when producing its result.</param>
public sealed record OllamaProcessStatus(
    bool IsInstalled,
    bool IsRunning,
    string? ExecutablePath,
    IReadOnlyList<OllamaProcessInfo> Processes,
    string ProcessSummary,
    string Message);
