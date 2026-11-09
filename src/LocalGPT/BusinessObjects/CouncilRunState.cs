using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents council run state exchanged or persisted by the surrounding application workflow, with each member describing one part of that state.
/// </summary>
internal sealed class CouncilRunState
{
    /// <summary>
    /// Initializes a new <see cref="CouncilRunState"/> instance and captures the dependencies or initial state required by its council run workflow.
    /// </summary>
    /// <param name="runId">Identifier of the run to use for this operation.</param>
    /// <param name="participants">String dependency used by the council run workflow to provide the corresponding application capability.</param>
    /// <param name="modelRoutes">One wire council model route dependency used by the council run workflow to provide the corresponding application capability.</param>
    /// <param name="resourceLoadPercent">Resource load percent value supplied to the council run operation and used when producing its result.</param>
    /// <param name="requestedMaxOutputTokens">Requested max output tokens value supplied to the council run operation and used when producing its result.</param>
    /// <param name="requestedMaxContextTokens">Requested max context tokens value supplied to the council run operation and used when producing its result.</param>
    /// <param name="fallbackOllamaNumGpu">Fallback ollama num gpu value supplied to the council run operation and used when producing its result.</param>
    /// <param name="allowParallelHardwareRoads">Value indicating whether allow parallel hardware roads should apply to this operation.</param>
    /// <param name="maxParallelModels">Max parallel models value supplied to the council run operation and used when producing its result.</param>
    /// <param name="modelTimeoutSeconds">Model timeout seconds value supplied to the council run operation and used when producing its result.</param>
    public CouncilRunState(
        Guid runId,
        IEnumerable<string> participants,
        IEnumerable<OneWireCouncilModelRoute> modelRoutes,
        int resourceLoadPercent,
        int requestedMaxOutputTokens,
        int requestedMaxContextTokens,
        int? fallbackOllamaNumGpu,
        bool allowParallelHardwareRoads,
        int maxParallelModels,
        int modelTimeoutSeconds)
    {
        RunId = runId;
        Participants = participants.ToList();
        ModelRoutes = modelRoutes.ToList();
        ResourceLoadPercent = Math.Clamp((int)Math.Round(resourceLoadPercent / 5d) * 5, 0, 100);
        RequestedMaxOutputTokens = Math.Max(1, requestedMaxOutputTokens);
        RequestedMaxContextTokens = Math.Max(256, requestedMaxContextTokens);
        FallbackOllamaNumGpu = fallbackOllamaNumGpu;
        AllowParallelHardwareRoads = allowParallelHardwareRoads;
        MaxParallelModels = Math.Max(1, maxParallelModels);
        ModelTimeoutSeconds = Math.Clamp(modelTimeoutSeconds, 30, 1800);
        ChangeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    /// <summary>
    /// Gets the sync root value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The sync root value exposed by <see cref="CouncilRunState"/>.</value>
    public object SyncRoot { get; } = new();
    /// <summary>
    /// Gets the stable run identifier used to identify or correlate this council run instance with related application state.
    /// </summary>
    /// <value>The run identifier value exposed by <see cref="CouncilRunState"/>.</value>
    public Guid RunId { get; }
    /// <summary>
    /// Gets or sets the revision value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="CouncilRunState"/>.</value>
    public long Revision { get; set; } = 1;
    /// <summary>
    /// Gets or sets the participants collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The participants value exposed by <see cref="CouncilRunState"/>.</value>
    public List<string> Participants { get; set; }
    /// <summary>
    /// Gets or sets the model routes collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The model routes value exposed by <see cref="CouncilRunState"/>.</value>
    public List<OneWireCouncilModelRoute> ModelRoutes { get; set; }
    /// <summary>
    /// Gets or sets the resource load percent value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The resource load percent value exposed by <see cref="CouncilRunState"/>.</value>
    public int ResourceLoadPercent { get; set; }
    /// <summary>
    /// Gets or sets the requested max output tokens value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested max output tokens value exposed by <see cref="CouncilRunState"/>.</value>
    public int RequestedMaxOutputTokens { get; set; }
    /// <summary>
    /// Gets or sets the requested max context tokens value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The requested max context tokens value exposed by <see cref="CouncilRunState"/>.</value>
    public int RequestedMaxContextTokens { get; set; }
    /// <summary>
    /// Gets or sets the fallback Ollama num GPU value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The fallback Ollama num GPU value exposed by <see cref="CouncilRunState"/>.</value>
    public int? FallbackOllamaNumGpu { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether parallel hardware roads applies to the council run state.
    /// </summary>
    /// <value>The allow parallel hardware roads value exposed by <see cref="CouncilRunState"/>.</value>
    public bool AllowParallelHardwareRoads { get; set; }
    /// <summary>Gets or sets the per-host Council model request concurrency ceiling used by road-parallel scheduling.</summary>
    /// <value>The max parallel models value exposed by <see cref="CouncilRunState"/>.</value>
    public int MaxParallelModels { get; set; }
    /// <summary>Gets or sets the provider-request timeout in seconds for Council members in this run.</summary>
    /// <value>The model timeout seconds value exposed by <see cref="CouncilRunState"/>.</value>
    public int ModelTimeoutSeconds { get; set; }
    /// <summary>Gets or sets the Council team key captured when the live run began.</summary>
    /// <value>The service-backed Council team key.</value>
    public string CouncilTeamKey { get; set; } = "general";
    /// <summary>Gets or sets the saved model preset identity captured when the live run began.</summary>
    /// <value>The optional model preset identifier.</value>
    public Guid? ModelPresetId { get; set; }
    /// <summary>Gets or sets the saved hardware performance preset identity captured when the live run began.</summary>
    /// <value>The optional hardware performance preset identifier.</value>
    public Guid? HardwarePerformancePresetId { get; set; }
    /// <summary>Gets or sets the critique-round count captured when the live run began.</summary>
    /// <value>The configured number of critique rounds.</value>
    public int CritiqueRounds { get; set; }
    /// <summary>Gets or sets a value indicating whether chat memory was included when the live run began.</summary>
    /// <value><see langword="true"/> when chat memory was included.</value>
    public bool IncludeMemory { get; set; }
    /// <summary>Gets or sets a value indicating whether automatic project creation was enabled when the live run began.</summary>
    /// <value><see langword="true"/> when automatic project creation was enabled.</value>
    public bool CreateProjectPerRun { get; set; }
    /// <summary>
    /// Gets or sets the current round value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current round value exposed by <see cref="CouncilRunState"/>.</value>
    public int CurrentRound { get; set; } = -1;
    /// <summary>
    /// Gets or sets the current phase value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The current phase value exposed by <see cref="CouncilRunState"/>.</value>
    public string CurrentPhase { get; set; } = "Preparing";
    /// <summary>
    /// Gets or sets a value indicating whether round skip requested applies to the council run state.
    /// </summary>
    /// <value>The is round skip requested value exposed by <see cref="CouncilRunState"/>.</value>
    public bool IsRoundSkipRequested { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether running applies to the council run state.
    /// </summary>
    /// <value>The is running value exposed by <see cref="CouncilRunState"/>.</value>
    public bool IsRunning { get; set; } = true;
    /// <summary>
    /// Gets the active lane counts collection maintained or exposed by this council run instance for downstream processing.
    /// </summary>
    /// <value>The active lane counts value exposed by <see cref="CouncilRunState"/>.</value>
    public Dictionary<string, int> ActiveLaneCounts { get; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Gets or sets the change signal value that forms part of the council run state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The change signal value exposed by <see cref="CouncilRunState"/>.</value>
    public TaskCompletionSource<bool> ChangeSignal { get; set; }
    /// <summary>
    /// Gets or sets the cancellation signal used to stop or abandon work associated with this council run operation.
    /// </summary>
    /// <value>The round cancellation value exposed by <see cref="CouncilRunState"/>.</value>
    public CancellationTokenSource RoundCancellation { get; set; } = new();
}
