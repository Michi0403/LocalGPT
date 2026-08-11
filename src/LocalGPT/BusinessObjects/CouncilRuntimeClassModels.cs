using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Lists supported runtime class kind values.
/// </summary>
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

/// <summary>
/// Lists supported runtime field input mode values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeFieldInputMode
{
    Ai,
    HumanOptional,
    HumanRequired,
    Shared,
    System
}

/// <summary>
/// Represents a runtime class field definition.
/// </summary>
public sealed class RuntimeClassFieldDefinition
{
    /// <summary>
    /// Gets or sets name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets data type.
    /// </summary>
    public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets default value.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets input mode.
    /// </summary>
    public RuntimeFieldInputMode InputMode { get; set; } = RuntimeFieldInputMode.Ai;
    /// <summary>
    /// Gets or sets ai assignable.
    /// </summary>
    public bool AiAssignable { get; set; } = true;
    /// <summary>
    /// Gets or sets human assignable.
    /// </summary>
    public bool HumanAssignable { get; set; }
    /// <summary>
    /// Gets or sets blocks next round until human input.
    /// </summary>
    public bool BlocksNextRoundUntilHumanInput { get; set; }
    /// <summary>
    /// Gets or sets is required.
    /// </summary>
    public bool IsRequired { get; set; }
    /// <summary>
    /// Gets or sets keyboard key.
    /// </summary>
    public string KeyboardKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gamepad button.
    /// </summary>
    public string GamepadButton { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets allowed values JSON.
    /// </summary>
    public string AllowedValuesJson { get; set; } = "[]";
}

/// <summary>
/// Represents a runtime input binding definition.
/// </summary>
public sealed class RuntimeInputBindingDefinition
{
    /// <summary>
    /// Gets or sets action.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets keyboard key.
    /// </summary>
    public string KeyboardKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets gamepad button.
    /// </summary>
    public string GamepadButton { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a council runtime class definition.
/// </summary>
public sealed class CouncilRuntimeClassDefinition
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public RuntimeClassKind Kind { get; set; }
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fields.
    /// </summary>
    public List<RuntimeClassFieldDefinition> Fields { get; set; } = [];
    /// <summary>
    /// Gets or sets input bindings.
    /// </summary>
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets aliases.
    /// </summary>
    public List<string> Aliases { get; set; } = [];
    /// <summary>
    /// Gets or sets recommended DevExpress functions.
    /// </summary>
    public List<string> RecommendedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets source references.
    /// </summary>
    public List<string> SourceReferences { get; set; } = [];
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is system seed.
    /// </summary>
    public bool IsSystemSeed { get; set; }
    /// <summary>
    /// Gets or sets is user modified.
    /// </summary>
    public bool IsUserModified { get; set; }
}

/// <summary>
/// Represents a save council runtime class request.
/// </summary>
public sealed class SaveCouncilRuntimeClassRequest
{
    /// <summary>
    /// Gets or sets definition.
    /// </summary>
    public CouncilRuntimeClassDefinition Definition { get; set; } = new();
    /// <summary>
    /// Gets or sets user confirmed.
    /// </summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Represents a council runtime class configuration.
/// </summary>
public sealed class CouncilRuntimeClassConfiguration
{
    /// <summary>
    /// Gets or sets identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets key.
    /// </summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets namespace.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = RuntimeClassKind.State.ToString();
    /// <summary>
    /// Gets or sets description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets fields JSON.
    /// </summary>
    public string FieldsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets input bindings JSON.
    /// </summary>
    public string InputBindingsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets recommended DevExpress functions JSON.
    /// </summary>
    public string RecommendedDxFunctionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets source references JSON.
    /// </summary>
    public string SourceReferencesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets is system seed.
    /// </summary>
    public bool IsSystemSeed { get; set; }
    /// <summary>
    /// Gets or sets is user modified.
    /// </summary>
    public bool IsUserModified { get; set; }
    /// <summary>
    /// Gets or sets seed version.
    /// </summary>
    public int SeedVersion { get; set; }
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
