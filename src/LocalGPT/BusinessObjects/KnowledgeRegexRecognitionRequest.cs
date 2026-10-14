namespace LocalGPT.BusinessObjects;

/// <summary>Transient user-supplied recognition input. Test text is not persisted by the regex relationship service.</summary>
public sealed class KnowledgeRegexRecognitionRequest
{
    /// <summary>Bounded transient text evaluated against enabled knowledge-linked regex rules without persistence.</summary>
    /// <value>Bounded transient text evaluated against enabled knowledge-linked regex rules without persistence.</value>
    public string Text { get; set; } = string.Empty;
}
