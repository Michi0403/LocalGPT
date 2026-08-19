using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Defines the supported runtime class kind values used to select or describe behavior in the surrounding workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeClassKind
{
    /// <summary>
    /// Selects the session option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Session,
    /// <summary>
    /// Selects the world option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    World,
    /// <summary>
    /// Selects the map option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Map,
    /// <summary>
    /// Selects the location option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Location,
    /// <summary>
    /// Selects the actor option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Actor,
    /// <summary>
    /// Selects the player option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Player,
    /// <summary>
    /// Selects the item option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Item,
    /// <summary>
    /// Selects the event option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Event,
    /// <summary>
    /// Selects the controller option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Controller,
    /// <summary>
    /// Selects the frame option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Frame,
    /// <summary>
    /// Selects the state option for <see cref="RuntimeClassKind"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    State
}

/// <summary>
/// Defines the supported runtime field input mode values used to select or describe behavior in the surrounding workflow.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuntimeFieldInputMode
{
    /// <summary>
    /// Selects the AI option for <see cref="RuntimeFieldInputMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Ai,
    /// <summary>
    /// Selects the human optional option for <see cref="RuntimeFieldInputMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HumanOptional,
    /// <summary>
    /// Selects the human required option for <see cref="RuntimeFieldInputMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    HumanRequired,
    /// <summary>
    /// Selects the shared option for <see cref="RuntimeFieldInputMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    Shared,
    /// <summary>
    /// Selects the system option for <see cref="RuntimeFieldInputMode"/>, giving callers a named value for that supported mode or state.
    /// </summary>
    System
}

/// <summary>
/// Represents a runtime class field definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RuntimeClassFieldDefinition
{
    /// <summary>
    /// Gets or sets the name value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the data type value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The data type value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string DataType { get; set; } = "string";
    /// <summary>
    /// Gets or sets the default value value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The default value value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string DefaultValue { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the input mode value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input mode value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public RuntimeFieldInputMode InputMode { get; set; } = RuntimeFieldInputMode.Ai;
    /// <summary>
    /// Gets or sets a value indicating whether AI assignable applies to the runtime class field definition state.
    /// </summary>
    /// <value>The AI assignable value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public bool AiAssignable { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether human assignable applies to the runtime class field definition state.
    /// </summary>
    /// <value>The human assignable value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public bool HumanAssignable { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether blocks next round until human input applies to the runtime class field definition state.
    /// </summary>
    /// <value>The blocks next round until human input value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public bool BlocksNextRoundUntilHumanInput { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether required applies to the runtime class field definition state.
    /// </summary>
    /// <value>The is required value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public bool IsRequired { get; set; }
    /// <summary>
    /// Gets or sets the stable keyboard key used to identify or correlate this runtime class field definition instance with related application state.
    /// </summary>
    /// <value>The keyboard key value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string KeyboardKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the gamepad button value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The gamepad button value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string GamepadButton { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the allowed values JSON value that forms part of the runtime class field definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed values JSON value exposed by <see cref="RuntimeClassFieldDefinition"/>.</value>
    public string AllowedValuesJson { get; set; } = "[]";
}

/// <summary>
/// Represents a runtime input binding definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class RuntimeInputBindingDefinition
{
    /// <summary>
    /// Gets or sets the action value that forms part of the runtime input binding definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The action value exposed by <see cref="RuntimeInputBindingDefinition"/>.</value>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the runtime input binding definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="RuntimeInputBindingDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable keyboard key used to identify or correlate this runtime input binding definition instance with related application state.
    /// </summary>
    /// <value>The keyboard key value exposed by <see cref="RuntimeInputBindingDefinition"/>.</value>
    public string KeyboardKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the gamepad button value that forms part of the runtime input binding definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The gamepad button value exposed by <see cref="RuntimeInputBindingDefinition"/>.</value>
    public string GamepadButton { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the description value that forms part of the runtime input binding definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="RuntimeInputBindingDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Represents a council runtime class definition application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
public sealed class CouncilRuntimeClassDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council runtime class definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council runtime class definition instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the namespace value that forms part of the council runtime class definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The namespace value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the council runtime class definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the council runtime class definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public RuntimeClassKind Kind { get; set; }
    /// <summary>
    /// Gets or sets the description value that forms part of the council runtime class definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fields collection maintained or exposed by this council runtime class definition instance for downstream processing.
    /// </summary>
    /// <value>The fields value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public List<RuntimeClassFieldDefinition> Fields { get; set; } = [];
    /// <summary>
    /// Gets or sets the input bindings collection maintained or exposed by this council runtime class definition instance for downstream processing.
    /// </summary>
    /// <value>The input bindings value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public List<RuntimeInputBindingDefinition> InputBindings { get; set; } = [];
    /// <summary>
    /// Gets or sets the aliases collection maintained or exposed by this council runtime class definition instance for downstream processing.
    /// </summary>
    /// <value>The aliases value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public List<string> Aliases { get; set; } = [];
    /// <summary>
    /// Gets or sets the recommended DevExpress functions collection maintained or exposed by this council runtime class definition instance for downstream processing.
    /// </summary>
    /// <value>The recommended DevExpress functions value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public List<string> RecommendedDxFunctions { get; set; } = [];
    /// <summary>
    /// Gets or sets the source references collection maintained or exposed by this council runtime class definition instance for downstream processing.
    /// </summary>
    /// <value>The source references value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public List<string> SourceReferences { get; set; } = [];
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council runtime class definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether system seed applies to the council runtime class definition state.
    /// </summary>
    /// <value>The is system seed value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public bool IsSystemSeed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user modified applies to the council runtime class definition state.
    /// </summary>
    /// <value>The is user modified value exposed by <see cref="CouncilRuntimeClassDefinition"/>.</value>
    public bool IsUserModified { get; set; }
}

/// <summary>
/// Represents the input contract for save council runtime class, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class SaveCouncilRuntimeClassRequest
{
    /// <summary>
    /// Gets or sets the definition value that forms part of the save council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The definition value exposed by <see cref="SaveCouncilRuntimeClassRequest"/>.</value>
    public CouncilRuntimeClassDefinition Definition { get; set; } = new();
    /// <summary>
    /// Gets or sets a value indicating whether user confirmed applies to the save council runtime class state.
    /// </summary>
    /// <value>The user confirmed value exposed by <see cref="SaveCouncilRuntimeClassRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>
/// Carries the configurable council runtime class settings used to control the associated application behavior without hard-coding policy in consumers.
/// </summary>
public sealed class CouncilRuntimeClassConfiguration
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this council runtime class instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the stable key used to identify or correlate this council runtime class instance with related application state.
    /// </summary>
    /// <value>The key value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string Key { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the namespace value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The namespace value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string Namespace { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string Kind { get; set; } = RuntimeClassKind.State.ToString();
    /// <summary>
    /// Gets or sets the description value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The description value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string Description { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the fields JSON value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fields JSON value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string FieldsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the input bindings JSON value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The input bindings JSON value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string InputBindingsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the recommended DevExpress functions JSON value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The recommended DevExpress functions JSON value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string RecommendedDxFunctionsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the source references JSON value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source references JSON value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public string SourceReferencesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the council runtime class state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether system seed applies to the council runtime class state.
    /// </summary>
    /// <value>The is system seed value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public bool IsSystemSeed { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether user modified applies to the council runtime class state.
    /// </summary>
    /// <value>The is user modified value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public bool IsUserModified { get; set; }
    /// <summary>
    /// Gets or sets the seed version value that forms part of the council runtime class state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The seed version value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public int SeedVersion { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this council runtime class state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this council runtime class state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="CouncilRuntimeClassConfiguration"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
