namespace LocalGPT.BusinessObjects;

/// <summary>Paginated discovery of public service methods in the existing database-backed DX Function Catalog.</summary>
public sealed class PublicServiceControllerPage
{
    /// <summary>Total catalogued methods matching the current search and service filter.</summary>
    /// <value>Total catalogued methods matching the current search and service filter.</value>
    public int Total { get; set; }
    /// <summary>Zero-based catalog offset of the current page.</summary>
    /// <value>Zero-based catalog offset of the current page.</value>
    public int Skip { get; set; }
    /// <summary>Maximum number of methods requested on the current page.</summary>
    /// <value>Maximum number of methods requested on the current page.</value>
    public int Take { get; set; }
    /// <summary>Exact catalog entries with schema, stable keys, and current saved execution policy.</summary>
    /// <value>Exact catalog entries with schema, stable keys, and current saved execution policy.</value>
    public IReadOnlyList<DxAiFunctionCatalogEntry> Methods { get; set; } = [];
}

/// <summary>API metadata for a service contract property. Describes the CLR contract without evaluating getters or mutating objects.</summary>
public sealed class PublicServicePropertyDescriptor
{
    /// <summary>Public CLR property name on the configured service contract.</summary>
    /// <value>Public CLR property name on the configured service contract.</value>
    public string Name { get; set; } = string.Empty;
    /// <summary>Declared CLR property type identity for client tooling.</summary>
    /// <value>Declared CLR property type identity for client tooling.</value>
    public string TypeName { get; set; } = string.Empty;
    /// <summary>Whether the property declares a public getter; getters are never invoked during discovery.</summary>
    /// <value>Whether the property declares a public getter; getters are never invoked during discovery.</value>
    public bool CanRead { get; set; }
    /// <summary>Whether the property declares a public setter; discovery never invokes it.</summary>
    /// <value>Whether the property declares a public setter; discovery never invokes it.</value>
    public bool CanWrite { get; set; }
}

/// <summary>A purpose-grouped service capability discovery record; executable operations remain individually catalogued and approval-gated.</summary>
public sealed class PublicServiceGroupDescriptor
{
    /// <summary>Stable business-purpose service identity used by the catalog.</summary>
    /// <value>Stable business-purpose service identity used by the catalog.</value>
    public string Service { get; set; } = string.Empty;
    /// <summary>Number of indexed public methods belonging to this service type.</summary>
    /// <value>Number of indexed public methods belonging to this service type.</value>
    public int MethodCount { get; set; }
    /// <summary>Metadata about the service contract properties; property values are not retrieved.</summary>
    /// <value>Metadata about the service contract properties; property values are not retrieved.</value>
    public IReadOnlyList<PublicServicePropertyDescriptor> Properties { get; set; } = [];
}

/// <summary>A deliberately user-approved, purpose-grouped update to the persisted AI/peer exposure of public service methods.</summary>
public sealed class PublicServiceExposureRequest
{
    /// <summary>Stable business-purpose service identity used by the catalog.</summary>
    /// <value>Stable business-purpose service identity used by the catalog.</value>
    public string Service { get; set; } = string.Empty;
    /// <summary>Whether the selected service family should be offered to AI chat.</summary>
    /// <value>Whether the selected service family should be offered to AI chat.</value>
    public bool ExposeToAiChat { get; set; }
    /// <summary>Whether the selected service family should be exposed to approved 1-Wire peers.</summary>
    /// <value>Whether the selected service family should be exposed to approved 1-Wire peers.</value>
    public bool ExposeToOneWire { get; set; }
    /// <summary>Whether the selected service family allows an approved remote caller.</summary>
    /// <value>Whether the selected service family allows an approved remote caller.</value>
    public bool AllowRemoteInvocation { get; set; }
}

/// <summary>Reports the number of matching and successfully updated methods in one group; cancellation stops further saves.</summary>
public sealed class PublicServiceExposureResult
{
    /// <summary>Stable business-purpose service identity used by the catalog.</summary>
    /// <value>Stable business-purpose service identity used by the catalog.</value>
    public string Service { get; set; } = string.Empty;
    /// <summary>Number of catalog entries with the exact selected service identity.</summary>
    /// <value>Number of catalog entries with the exact selected service identity.</value>
    public int Matched { get; set; }
    /// <summary>Number of entries whose persisted exposure policy changed.</summary>
    /// <value>Number of entries whose persisted exposure policy changed.</value>
    public int Updated { get; set; }
}
