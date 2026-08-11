using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Database-backed, user-editable policy and discovery record for a DX function or public application service method.
/// Descriptor metadata is refreshed from the running application while user policy fields are preserved.
/// </summary>
public sealed class DxAiFunctionCatalogEntry
{
    /// <summary>
    /// Gets or sets catalog key.
    /// </summary>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets kind.
    /// </summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets function name.
    /// </summary>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets purpose.
    /// </summary>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets method.
    /// </summary>
    public string Method { get; set; } = "POST";
    /// <summary>
    /// Gets or sets route.
    /// </summary>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameter schema JSON.
    /// </summary>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>
    /// Gets or sets source.
    /// </summary>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets service contract type name.
    /// </summary>
    public string ServiceContractTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets implementation type name.
    /// </summary>
    public string ImplementationTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets service method name.
    /// </summary>
    public string ServiceMethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameter type names JSON.
    /// </summary>
    public string ParameterTypeNamesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets is read only.
    /// </summary>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets is available.
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets expose to ai chat.
    /// </summary>
    public bool ExposeToAiChat { get; set; } = true;
    /// <summary>
    /// Gets or sets expose to one wire.
    /// </summary>
    public bool ExposeToOneWire { get; set; }
    /// <summary>
    /// Gets or sets allow remote invocation.
    /// </summary>
    public bool AllowRemoteInvocation { get; set; }
    /// <summary>
    /// Gets or sets requires frontend confirmation.
    /// </summary>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets interaction editor.
    /// </summary>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>
    /// Gets or sets allowed peer identifiers JSON.
    /// </summary>
    public string AllowedPeerIdsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets descriptor hash.
    /// </summary>
    public string DescriptorHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is system seed.
    /// </summary>
    public bool IsSystemSeed { get; set; } = true;
    /// <summary>
    /// Gets or sets created at UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated at UTC.
    /// </summary>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets updated by.
    /// </summary>
    public string UpdatedBy { get; set; } = "LocalGPT runtime catalog";

    /// <summary>
    /// Gets allowed peer identifiers.
    /// </summary>
    public IReadOnlyList<string> GetAllowedPeerIds()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(AllowedPeerIdsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>
    /// Runs the allows peer operation.
    /// </summary>
    public bool AllowsPeer(string peerId)
    {
        var peers = GetAllowedPeerIds();
        return peers.Count == 0 || peers.Any(item => string.Equals(item, peerId, StringComparison.OrdinalIgnoreCase));
    }
}


/// <summary>
/// Represents a DevExpress ai function catalog save request.
/// </summary>
public sealed class DxAiFunctionCatalogSaveRequest
{
    /// <summary>
    /// Gets or sets catalog key.
    /// </summary>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets expose to ai chat.
    /// </summary>
    public bool ExposeToAiChat { get; set; }
    /// <summary>
    /// Gets or sets expose to one wire.
    /// </summary>
    public bool ExposeToOneWire { get; set; }
    /// <summary>
    /// Gets or sets allow remote invocation.
    /// </summary>
    public bool AllowRemoteInvocation { get; set; }
    /// <summary>
    /// Gets or sets requires frontend confirmation.
    /// </summary>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets interaction editor.
    /// </summary>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>
    /// Gets or sets allowed peer identifiers JSON.
    /// </summary>
    public string AllowedPeerIdsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets updated by.
    /// </summary>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>
/// Represents a public service method invocation request.
/// </summary>
public sealed class PublicServiceMethodInvocationRequest
{
    /// <summary>
    /// Gets or sets catalog key.
    /// </summary>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets parameters.
    /// </summary>
    public JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets requested by.
    /// </summary>
    public string RequestedBy { get; set; } = "CurrentUser";
}
