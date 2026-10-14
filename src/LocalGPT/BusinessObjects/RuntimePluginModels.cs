namespace LocalGPT.BusinessObjects;

/// <summary>Identifies how a persisted user runtime extension is materialized and invoked.</summary>
public enum RuntimePluginKind
{
    /// <summary>A compact C# script body compiled into the shared LocalGPT plugin contract.</summary>
    CSharpScript = 0,
    /// <summary>A JavaScript module executed by a configured Node-compatible toolchain.</summary>
    JavaScript = 1,
    /// <summary>An approved Python script executed in an isolated configured CPython process, not via embedded Python.NET.</summary>
    Python = 3,
    /// <summary>A trusted precompiled .NET plugin package loaded in an isolated assembly load context.</summary>
    CompiledAssembly = 2
}

/// <summary>Database-backed runtime extension definition that can expose one dynamic DXFunction.</summary>
public sealed class RuntimePluginDefinition
{
    /// <summary>
    /// Gets or sets the stable identifier used to identify or correlate this runtime plugin definition instance with related application state.
    /// </summary>
    /// <value>The identifier value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Gets or sets the name value that forms part of the runtime plugin definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The name value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the dynamic function name. Runtime plugins use the plugin.* namespace.</summary>
    /// <value>The function name value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the purpose shown to people and AI clients.</summary>
    /// <value>The purpose value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the safety notes value that forms part of the runtime plugin definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The safety notes value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON schema used to validate invocation parameters.</summary>
    /// <value>The parameter schema JSON value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}";
    /// <summary>
    /// Gets or sets the kind value that forms part of the runtime plugin definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public RuntimePluginKind Kind { get; set; } = RuntimePluginKind.CSharpScript;
    /// <summary>
    /// Gets or sets the source code value that forms part of the runtime plugin definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source code value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string SourceCode { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional base64 ZIP containing a trusted compiled plugin and its dependencies.</summary>
    /// <value>The package payload base64 value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string PackagePayloadBase64 { get; set; } = string.Empty;
    /// <summary>Gets or sets the entry assembly filename inside a compiled plugin package.</summary>
    /// <value>The entry assembly name value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string EntryAssemblyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the fully-qualified plugin entry type implementing the shared contract.</summary>
    /// <value>The entry type name value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string EntryTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the stable compiler installation identifier used to identify or correlate this runtime plugin definition instance with related application state.
    /// </summary>
    /// <value>The compiler installation identifier value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public Guid? CompilerInstallationId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the runtime plugin definition state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the function descriptor is exposed to AI clients.</summary>
    /// <value>The available to AI value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public bool AvailableToAi { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the runtime plugin definition state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public bool IsReadOnly { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether requires human confirmation applies to the runtime plugin definition state.
    /// </summary>
    /// <value>The requires human confirmation value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public bool RequiresHumanConfirmation { get; set; } = true;
    /// <summary>Gets or sets whether automatic invocation may be considered by the normal registry policy.</summary>
    /// <value>The supports automatic invocation value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public bool SupportsAutomaticInvocation { get; set; }
    /// <summary>Gets or sets the SHA-256 hash of the last materialized source/package.</summary>
    /// <value>The content hash value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>Gets or sets the last build/load state.</summary>
    /// <value>The last build status value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string LastBuildStatus { get; set; } = "NotBuilt";
    /// <summary>
    /// Gets or sets the last build message value that forms part of the runtime plugin definition state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The last build message value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public string LastBuildMessage { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the last loaded at UTC associated with this runtime plugin definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The last loaded at UTC value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public DateTime? LastLoadedAtUtc { get; set; }
    /// <summary>
    /// Gets or sets the created at UTC associated with this runtime plugin definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this runtime plugin definition state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="RuntimePluginDefinition"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Carries an explicitly confirmed runtime extension mutation.</summary>
public sealed class SaveRuntimePluginRequest
{
    /// <summary>
    /// Gets or sets the plugin value that forms part of the save runtime plugin state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plugin value exposed by <see cref="SaveRuntimePluginRequest"/>.</value>
    public RuntimePluginDefinition Plugin { get; set; } = new();
    /// <summary>Gets or sets whether the local user confirmed persistence of executable content.</summary>
    /// <value>The user confirmed value exposed by <see cref="SaveRuntimePluginRequest"/>.</value>
    public bool UserConfirmed { get; set; }
}

/// <summary>Reports a runtime extension materialization/build/load operation.</summary>
public sealed class RuntimePluginBuildResult
{
    /// <summary>
    /// Gets or sets the stable plugin identifier used to identify or correlate this runtime plugin build instance with related application state.
    /// </summary>
    /// <value>The plugin identifier value exposed by <see cref="RuntimePluginBuildResult"/>.</value>
    public Guid PluginId { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded applies to the runtime plugin build state.
    /// </summary>
    /// <value>The succeeded value exposed by <see cref="RuntimePluginBuildResult"/>.</value>
    public bool Succeeded { get; set; }
    /// <summary>Gets or sets the resulting state.</summary>
    /// <value>The status value exposed by <see cref="RuntimePluginBuildResult"/>.</value>
    public string Status { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the message value that forms part of the runtime plugin build state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The message value exposed by <see cref="RuntimePluginBuildResult"/>.</value>
    public string Message { get; set; } = string.Empty;
}
