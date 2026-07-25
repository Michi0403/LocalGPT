namespace LocalGPT.Services;

public sealed record DxaichatFunctionInfo(
    string Name,
    string Method,
    string Route,
    string Purpose,
    string Parameters,
    string SafetyNotes,
    bool IsReadOnly = true,
    bool AvailableToAi = true,
    bool RequiresHumanConfirmation = false,
    bool SupportsDirectInvocation = false,
    bool SupportsAutomaticInvocation = false,
    string Source = "ControllerRoute",
    string ParameterSchemaJson = "{\"type\":\"object\",\"properties\":{}}");
