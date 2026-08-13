namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a component activity snapshot application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="TimestampUtc">Timestamp utc value supplied to the component activity snapshot operation and used when producing its result.</param>
/// <param name="Component">Component value supplied to the component activity snapshot operation and used when producing its result.</param>
/// <param name="Operation">Operation value supplied to the component activity snapshot operation and used when producing its result.</param>
/// <param name="Status">Status value supplied to the component activity snapshot operation and used when producing its result.</param>
/// <param name="Summary">Summary value supplied to the component activity snapshot operation and used when producing its result.</param>
/// <param name="Route">Route value supplied to the component activity snapshot operation and used when producing its result.</param>
public sealed record ComponentActivitySnapshot(
    DateTimeOffset TimestampUtc,
    string Component,
    string Operation,
    string Status,
    string Summary,
    string? Route);
