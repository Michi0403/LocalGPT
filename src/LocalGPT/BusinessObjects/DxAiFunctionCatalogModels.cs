using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Database-backed, user-editable policy and discovery record for a DX function or public application service method.
/// Descriptor metadata is refreshed from the running application while user policy fields are preserved.
/// </summary>
public sealed class DxAiFunctionCatalogEntry
{
    /// <summary>
    /// Gets or sets the stable catalog key used to identify or correlate this DevExpress AI function catalog instance with related application state.
    /// </summary>
    /// <value>The catalog key value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the kind value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The kind value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string Kind { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the function name value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The function name value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string FunctionName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the display name value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The display name value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string DisplayName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the purpose value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The purpose value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string Purpose { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the method value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The method value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string Method { get; set; } = "POST";
    /// <summary>
    /// Gets or sets the route value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The route value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string Route { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter schema JSON value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter schema JSON value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    /// <summary>
    /// Gets or sets the source value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The source value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string Source { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the service contract type name value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service contract type name value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string ServiceContractTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the implementation type name value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The implementation type name value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string ImplementationTypeName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the service method name value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The service method name value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string ServiceMethodName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameter type names JSON value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameter type names JSON value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string ParameterTypeNamesJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets a value indicating whether read only applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The is read only value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool IsReadOnly { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether available applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The is available value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool IsAvailable { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether expose to AI chat applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The expose to AI chat value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool ExposeToAiChat { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether expose to one wire applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The expose to one wire value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool ExposeToOneWire { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether remote invocation applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The allow remote invocation value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool AllowRemoteInvocation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires frontend confirmation applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The requires frontend confirmation value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets the interaction editor value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction editor value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>
    /// Gets or sets the allowed peer identifiers JSON value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed peer identifiers JSON value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string AllowedPeerIdsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the descriptor hash value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The descriptor hash value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string DescriptorHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether system seed applies to the DevExpress AI function catalog state.
    /// </summary>
    /// <value>The is system seed value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public bool IsSystemSeed { get; set; } = true;
    /// <summary>
    /// Gets or sets the created at UTC associated with this DevExpress AI function catalog state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The created at UTC value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated at UTC associated with this DevExpress AI function catalog state, using the time semantics implied by the member name.
    /// </summary>
    /// <value>The updated at UTC value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets the updated by value that forms part of the DevExpress AI function catalog state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The updated by value exposed by <see cref="DxAiFunctionCatalogEntry"/>.</value>
    public string UpdatedBy { get; set; } = "LocalGPT runtime catalog";

    /// <summary>
    /// Retrieves allowed peer identifiers for <see cref="DxAiFunctionCatalogEntry"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function catalog workflow.
    /// </summary>
    /// <returns>The collection produced by the operation.</returns>
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
    /// Performs allows peer for <see cref="DxAiFunctionCatalogEntry"/>, keeping the operation consistent with the state and invariants of the surrounding DevExpress AI function catalog workflow.
    /// </summary>
    /// <param name="peerId">Identifier of the peer to use for this operation.</param>
    /// <returns>A value indicating whether the requested condition or operation succeeded.</returns>
    public bool AllowsPeer(string peerId)
    {
        var peers = GetAllowedPeerIds();
        return peers.Count == 0 || peers.Any(item => string.Equals(item, peerId, StringComparison.OrdinalIgnoreCase));
    }
}


/// <summary>
/// Represents the input contract for DevExpress AI function catalog save, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class DxAiFunctionCatalogSaveRequest
{
    /// <summary>
    /// Gets or sets the stable catalog key used to identify or correlate this DevExpress AI function catalog save instance with related application state.
    /// </summary>
    /// <value>The catalog key value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets a value indicating whether enabled applies to the DevExpress AI function catalog save state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public bool IsEnabled { get; set; } = true;
    /// <summary>
    /// Gets or sets a value indicating whether expose to AI chat applies to the DevExpress AI function catalog save state.
    /// </summary>
    /// <value>The expose to AI chat value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public bool ExposeToAiChat { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether expose to one wire applies to the DevExpress AI function catalog save state.
    /// </summary>
    /// <value>The expose to one wire value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public bool ExposeToOneWire { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether remote invocation applies to the DevExpress AI function catalog save state.
    /// </summary>
    /// <value>The allow remote invocation value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public bool AllowRemoteInvocation { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether requires frontend confirmation applies to the DevExpress AI function catalog save state.
    /// </summary>
    /// <value>The requires frontend confirmation value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public bool RequiresFrontendConfirmation { get; set; } = true;
    /// <summary>
    /// Gets or sets the interaction editor value that forms part of the DevExpress AI function catalog save state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The interaction editor value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    /// <summary>
    /// Gets or sets the allowed peer identifiers JSON value that forms part of the DevExpress AI function catalog save state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The allowed peer identifiers JSON value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public string AllowedPeerIdsJson { get; set; } = "[]";
    /// <summary>
    /// Gets or sets the updated by value that forms part of the DevExpress AI function catalog save state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The updated by value exposed by <see cref="DxAiFunctionCatalogSaveRequest"/>.</value>
    public string UpdatedBy { get; set; } = "CurrentUser";
}

/// <summary>
/// Represents the input contract for public service method invocation, carrying the values a caller supplies to the corresponding application operation.
/// </summary>
public sealed class PublicServiceMethodInvocationRequest
{
    /// <summary>
    /// Gets or sets the stable catalog key used to identify or correlate this public service method invocation instance with related application state.
    /// </summary>
    /// <value>The catalog key value exposed by <see cref="PublicServiceMethodInvocationRequest"/>.</value>
    public string CatalogKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the parameters value that forms part of the public service method invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The parameters value exposed by <see cref="PublicServiceMethodInvocationRequest"/>.</value>
    public JsonElement Parameters { get; set; }
    /// <summary>
    /// Gets or sets the requested by value that forms part of the public service method invocation state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested by value exposed by <see cref="PublicServiceMethodInvocationRequest"/>.</value>
    public string RequestedBy { get; set; } = "CurrentUser";
}
