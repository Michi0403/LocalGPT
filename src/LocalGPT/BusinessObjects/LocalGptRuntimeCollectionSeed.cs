namespace LocalGPT.BusinessObjects;

/// <summary>
/// Seed row of one runtime collection, stored as a JSON string array in a <see cref="SystemVariable"/>.
/// </summary>
/// <param name="Name">Database key of the collection (SystemVariables.Name).</param>
/// <param name="Values">Default members written when the database row is missing.</param>
public sealed record LocalGptRuntimeCollectionSeed(string Name, IReadOnlyList<string> Values);
