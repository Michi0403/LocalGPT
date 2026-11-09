using System.Text.Json.Serialization;

namespace LocalGPT.BusinessObjects;

/// <summary>Represents one bounded prediction from a creature or reactive-object subdirector.</summary>
public sealed class CouncilGameSubdirectorPrediction
{
    /// <summary>
    /// Gets or sets the stable director key used to identify or correlate this council game subdirector prediction instance with related application state.
    /// </summary>
    /// <value>The director key value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string DirectorKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the actor kind value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The actor kind value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public CouncilGameActorKind ActorKind { get; set; }
    /// <summary>
    /// Gets or sets the stable runtime class key used to identify or correlate this council game subdirector prediction instance with related application state.
    /// </summary>
    /// <value>The runtime class key value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string RuntimeClassKey { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the prediction value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The prediction value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public string Prediction { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the confidence percent value that forms part of the council game subdirector prediction state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The confidence percent value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public int ConfidencePercent { get; set; }
    /// <summary>
    /// Gets or sets the actor instances collection maintained or exposed by this council game subdirector prediction instance for downstream processing.
    /// </summary>
    /// <value>The actor instances value exposed by <see cref="CouncilGameSubdirectorPrediction"/>.</value>
    public IReadOnlyList<CouncilGameActorRuntimeDescriptor> ActorInstances { get; set; } = [];
}
