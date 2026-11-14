namespace LocalGPT.BusinessObjects;

/// <summary>
/// One exact compare-and-set change of a repair option. The change is applied only while the stored value still
/// equals <see cref="ExpectedCurrentValue"/>; otherwise the whole option is rolled back and reported as changed state.
/// </summary>
public sealed class ConfigurationRepairChange
{
    /// <summary>Change kind for replacing one SystemVariables row value.</summary>
    public const string SystemVariableKind = "SystemVariable";

    /// <summary>Change kind for adding one item to a runtime collection stored as a JSON string array.</summary>
    public const string RuntimeCollectionAddKind = "RuntimeCollectionAdd";

    /// <summary>Change kind for replacing exact provider-qualified role bindings in a user-owned Council team.</summary>
    public const string CouncilTeamRolesKind = "CouncilTeamRoles";

    /// <summary>Gets or sets the change kind (<see cref="SystemVariableKind"/>, <see cref="RuntimeCollectionAddKind"/> or <see cref="CouncilTeamRolesKind"/>).</summary>
    /// <value>Change kind.</value>
    public string TargetKind { get; set; } = SystemVariableKind;

    /// <summary>Gets or sets the SystemVariables row name the change targets.</summary>
    /// <value>Target row name.</value>
    public string TargetKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the exact stored value observed when the finding was produced.</summary>
    /// <value>Expected current value; the change is refused when the stored value differs.</value>
    public string ExpectedCurrentValue { get; set; } = string.Empty;

    /// <summary>Gets or sets the value to store (or the collection item to add).</summary>
    /// <value>New value.</value>
    public string NewValue { get; set; } = string.Empty;
}
