namespace LocalGPT.BusinessObjects;

public sealed record ComponentActivitySnapshot(
    DateTimeOffset TimestampUtc,
    string Component,
    string Operation,
    string Status,
    string Summary,
    string? Route);
