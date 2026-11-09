namespace LocalGPT.BusinessObjects;

/// <summary>
/// Seed row of one runtime value (a <see cref="SystemVariable"/>), keyed by its database name.
/// </summary>
/// <param name="Name">Database key of the value (SystemVariables.Name).</param>
/// <param name="Value">Default value written when the database row is missing.</param>
/// <param name="DataType">CLR type name stored with the value (for example System.Int32 or System.String[]).</param>
public sealed record LocalGptRuntimeSystemVariableSeed(string Name, string Value, string DataType);
