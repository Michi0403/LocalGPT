namespace LocalGPT.BusinessObjects;

/// <summary>Authenticated request to read part of a generated PublisherStudio media file.</summary>
internal sealed record PublisherMediaReadRequest(Guid CouncilCorrelationId, string ArtifactId, long Offset, int Count);

/// <summary>Generated media metadata and the requested bytes, when generation has completed.</summary>
internal sealed record PublisherMediaReadResult(string Status, string ArtifactId = "", string FileName = "",
    string ContentType = "", long Length = 0, long Offset = 0, byte[]? Data = null);

/// <summary>Tracks the source peer and first successful, matching media artifact for a Council request.</summary>
internal sealed record PublisherMediaDeliveryRegistration(string PeerId, string ContentType, DateTimeOffset ExpiresUtc)
{
    internal LocalAiArtifactDescriptor? Artifact { get; set; }
}
