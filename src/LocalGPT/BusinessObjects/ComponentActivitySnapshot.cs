namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a component activity snapshot.
/// </summary>
public sealed record ComponentActivitySnapshot(
    DateTimeOffset TimestampUtc,
    string Component,
    string Operation,
    string Status,
    string Summary,
    string? Route);
