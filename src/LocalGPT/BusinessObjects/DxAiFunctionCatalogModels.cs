using System.Text.Json;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Database-backed, user-editable policy and discovery record for a DX function or public application service method.
/// Descriptor metadata is refreshed from the running application while user policy fields are preserved.
/// </summary>
public sealed class DxAiFunctionCatalogEntry
{
    public string CatalogKey { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Method { get; set; } = "POST";
    public string Route { get; set; } = string.Empty;
    public string ParameterSchemaJson { get; set; } = "{\"type\":\"object\",\"properties\":{}}";
    public string Source { get; set; } = string.Empty;
    public string ServiceContractTypeName { get; set; } = string.Empty;
    public string ImplementationTypeName { get; set; } = string.Empty;
    public string ServiceMethodName { get; set; } = string.Empty;
    public string ParameterTypeNamesJson { get; set; } = "[]";
    public bool IsReadOnly { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool ExposeToAiChat { get; set; } = true;
    public bool ExposeToOneWire { get; set; }
    public bool AllowRemoteInvocation { get; set; }
    public bool RequiresFrontendConfirmation { get; set; } = true;
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    public string AllowedPeerIdsJson { get; set; } = "[]";
    public string DescriptorHash { get; set; } = string.Empty;
    public bool IsSystemSeed { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "LocalGPT runtime catalog";

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

    public bool AllowsPeer(string peerId)
    {
        var peers = GetAllowedPeerIds();
        return peers.Count == 0 || peers.Any(item => string.Equals(item, peerId, StringComparison.OrdinalIgnoreCase));
    }
}


public sealed class DxAiFunctionCatalogSaveRequest
{
    public string CatalogKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool ExposeToAiChat { get; set; }
    public bool ExposeToOneWire { get; set; }
    public bool AllowRemoteInvocation { get; set; }
    public bool RequiresFrontendConfirmation { get; set; } = true;
    public OneWireInteractionEditor InteractionEditor { get; set; } = OneWireInteractionEditor.ConfirmationOnly;
    public string AllowedPeerIdsJson { get; set; } = "[]";
    public string UpdatedBy { get; set; } = "CurrentUser";
}

public sealed class PublicServiceMethodInvocationRequest
{
    public string CatalogKey { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
    public string RequestedBy { get; set; } = "CurrentUser";
}
