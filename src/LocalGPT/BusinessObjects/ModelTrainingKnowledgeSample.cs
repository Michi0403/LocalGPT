namespace LocalGPT.BusinessObjects;

/// <summary>Identifies the exact approved knowledge revision to use as a supervised answer.</summary>
public sealed record ModelTrainingKnowledgeSample(Guid KnowledgeId, string ContentSha256, string Prompt, bool Evaluation);
