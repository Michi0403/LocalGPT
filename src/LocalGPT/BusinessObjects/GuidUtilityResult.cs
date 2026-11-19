namespace LocalGPT.BusinessObjects;

/// <summary>Deterministic GUID computation receipt. This contract never attests database existence.</summary>
/// <param name="Valid">Whether the value is a nonempty GUID.</param>
/// <param name="Value">Canonical hyphenated text, or empty for invalid input.</param>
/// <param name="IsNew">Whether the service generated an unbound identifier.</param>
public sealed record GuidUtilityResult(bool Valid, string Value, bool IsNew);
