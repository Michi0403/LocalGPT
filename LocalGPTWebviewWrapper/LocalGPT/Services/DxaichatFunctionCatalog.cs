namespace LocalGPT.Services;

public sealed record DxaichatFunctionInfo(
    string Name,
    string Method,
    string Route,
    string Purpose,
    string Parameters,
    string SafetyNotes);
