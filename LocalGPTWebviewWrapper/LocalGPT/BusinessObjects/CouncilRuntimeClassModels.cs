using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeClassKind
{
    Session,
    World,
    Map,
    Location,
    Actor,
    Player,
    Item,
    Event,
    Controller,
    Frame,
    State
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeFieldInputMode
{
    Ai,
    HumanOptional,
    HumanRequired,
    Shared,
    System
}

public sealed class RuntimeClassFieldDefinition
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public string DefaultValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RuntimeFieldInputMode InputMode { get; set; } = RuntimeFieldInputMode.Ai;
    public bool AiAssignable { get; set; } = true;
    public bool HumanAssignable { get; set; }
    public bool BlocksNextRoundUntilHumanInput { get; set; }
    public bool IsRequired { get; set; }
    public string KeyboardKey { get; set; } = string.Empty;
    public string GamepadButton { get; set; } = string.Empty;
    public string AllowedValuesJson { get; set; } = "[]";
}

public sealed class RuntimeInputBindingDefinition
{
    public string Action { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string KeyboardKey { get; set; } = string.Empty;
    public string GamepadButton { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class CouncilRuntimeClassDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public RuntimeClassKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<RuntimeClassFieldDefinition> Fields { get; set; } = [];
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    public List<string> Aliases { get; set; } = [];
    public List<string> RecommendedDxFunctions { get; set; } = [];
    public List<string> SourceReferences { get; set; } = [];
    public bool IsEnabled { get; set; } = true;
    public bool IsSystemSeed { get; set; }
    public bool IsUserModified { get; set; }
}

public sealed class SaveCouncilRuntimeClassRequest
{
    public CouncilRuntimeClassDefinition Definition { get; set; } = new();
    public bool UserConfirmed { get; set; }
}

public sealed class CouncilRuntimeClassConfiguration
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Kind { get; set; } = RuntimeClassKind.State.ToString();
    public string Description { get; set; } = string.Empty;
    public string FieldsJson { get; set; } = "[]";
    public string InputBindingsJson { get; set; } = "[]";
    public string RecommendedDxFunctionsJson { get; set; } = "[]";
    public string SourceReferencesJson { get; set; } = "[]";
    public bool IsEnabled { get; set; } = true;
    public bool IsSystemSeed { get; set; }
    public bool IsUserModified { get; set; }
    public int SeedVersion { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
