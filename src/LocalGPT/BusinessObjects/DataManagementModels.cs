namespace LocalGPT.BusinessObjects;

/// <summary>One database-enforced foreign-key edge for navigating a table's parent or child records.</summary>
/// <param name="ChildTable">The dependent table.</param>
/// <param name="ParentTable">The referenced table.</param>
/// <param name="ChildColumn">The dependent column.</param>
/// <param name="ParentColumn">The referenced column.</param>
public sealed record DataManagementRelation(string ChildTable, string ParentTable, string ChildColumn, string ParentColumn)
{
    /// <summary>UI label for navigating to the declared parent table.</summary>
    /// <value>The parent navigation label value exposed by <see cref="DataManagementRelation"/>.</value>
    public string ParentNavigationLabel => $"↑ {ParentTable} · {ChildColumn}";

    /// <summary>UI label for navigating from a principal row to dependent records.</summary>
    /// <value>The child navigation label value exposed by <see cref="DataManagementRelation"/>.</value>
    public string ChildNavigationLabel => $"↓ {ChildTable} · {ChildColumn}";
}

/// <summary>Summarizes a live SQLite entity table and its actual relational neighbors.</summary>
/// <param name="Name">The table name.</param>
/// <param name="GroupKey">The semantic navigation group.</param>
/// <param name="RowCount">The current number of records.</param>
/// <param name="ColumnCount">The number of columns.</param>
/// <param name="Parents">Foreign keys from this table to parent tables.</param>
/// <param name="Children">Foreign keys from child tables to this table.</param>
public sealed record DataManagementTableInfo(
    string Name,
    string GroupKey,
    long RowCount,
    int ColumnCount,
    IReadOnlyList<DataManagementRelation> Parents,
    IReadOnlyList<DataManagementRelation> Children)
{
    /// <summary>Concise relational summary for the DevExpress navigation grid.</summary>
    /// <value>The relations value exposed by <see cref="DataManagementTableInfo"/>.</value>
    public string Relations => $"{Parents.Count} parents · {Children.Count} children";
}
