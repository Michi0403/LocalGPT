namespace LocalGPT.BusinessObjects;

/// <summary>Identifies how a persisted user runtime extension is materialized and invoked.</summary>
public enum RuntimePluginKind
{
    /// <summary>A compact C# script body compiled into the shared LocalGPT plugin contract.</summary>
    CSharpScript,
    /// <summary>A JavaScript module executed by a configured Node-compatible toolchain.</summary>
    JavaScript,
    /// <summary>A trusted precompiled .NET plugin package loaded in an isolated assembly load context.</summary>
    CompiledAssembly
}

/// <summary>Database-backed runtime extension definition that can expose one dynamic DXFunction.</summary>
public sealed class RuntimePluginDefinition
{
    /// <summary>Gets or sets the stable database identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Gets or sets the human-readable extension name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Gets or sets the dynamic function name. Runtime plugins use the plugin.* namespace.</summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>Gets or sets the purpose shown to people and AI clients.</summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>Gets or sets safety notes shown before consequential execution.</summary>
    public string SafetyNotes { get; set; } = string.Empty;
    /// <summary>Gets or sets the JSON schema used to validate invocation parameters.</summary>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{},\"additionalProperties\":true}";
    /// <summary>Gets or sets the runtime extension kind.</summary>
    public RuntimePluginKind Kind { get; set; } = RuntimePluginKind.CSharpScript;
    /// <summary>Gets or sets the persisted C# or JavaScript source text.</summary>
    public string SourceCode { get; set; } = string.Empty;
    /// <summary>Gets or sets an optional base64 ZIP containing a trusted compiled plugin and its dependencies.</summary>
    public string PackagePayloadBase64 { get; set; } = string.Empty;
    /// <summary>Gets or sets the entry assembly filename inside a compiled plugin package.</summary>
    public string EntryAssemblyName { get; set; } = string.Empty;
    /// <summary>Gets or sets the fully-qualified plugin entry type implementing the shared contract.</summary>
    public string EntryTypeName { get; set; } = string.Empty;
    /// <summary>Gets or sets the selected persisted compiler/runtime installation.</summary>
    public Guid? CompilerInstallationId { get; set; }
    /// <summary>Gets or sets whether this runtime extension is enabled.</summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>Gets or sets whether the function descriptor is exposed to AI clients.</summary>
    public bool AvailableToAi { get; set; } = true;
    /// <summary>Gets or sets whether the extension declares itself read-only.</summary>
    public bool IsReadOnly { get; set; } = true;
    /// <summary>Gets or sets whether invocation requires LocalGPT human approval.</summary>
    public bool RequiresHumanConfirmation { get; set; } = true;
    /// <summary>Gets or sets whether automatic invocation may be considered by the normal registry policy.</summary>
    public bool SupportsAutomaticInvocation { get; set; }
    /// <summary>Gets or sets the SHA-256 hash of the last materialized source/package.</summary>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>Gets or sets the last build/load state.</summary>
    public string LastBuildStatus { get; set; } = "NotBuilt";
    /// <summary>Gets or sets bounded build/load diagnostics.</summary>
    public string LastBuildMessage { get; set; } = string.Empty;
    /// <summary>Gets or sets when the extension last loaded successfully.</summary>
    public DateTime? LastLoadedAtUtc { get; set; }
    /// <summary>Gets or sets when the persisted definition was created.</summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>Gets or sets when the persisted definition was last updated.</summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Carries an explicitly confirmed runtime extension mutation.</summary>
public sealed class SaveRuntimePluginRequest
{
    /// <summary>Gets or sets the definition being created or updated.</summary>
    public RuntimePluginDefinition Plugin { get; set; } = new();
    /// <summary>Gets or sets whether the local user confirmed persistence of executable content.</summary>
    public bool UserConfirmed { get; set; }
}

/// <summary>Reports a runtime extension materialization/build/load operation.</summary>
public sealed class RuntimePluginBuildResult
{
    /// <summary>Gets or sets the plugin identifier.</summary>
    public Guid PluginId { get; set; }
    /// <summary>Gets or sets whether the operation succeeded.</summary>
    public bool Succeeded { get; set; }
    /// <summary>Gets or sets the resulting state.</summary>
    public string Status { get; set; } = string.Empty;
    /// <summary>Gets or sets bounded human-readable diagnostics.</summary>
    public string Message { get; set; } = string.Empty;
}
