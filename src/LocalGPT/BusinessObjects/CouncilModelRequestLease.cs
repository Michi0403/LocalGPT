using System.Text;
using LocalGPT.Interfaces;

namespace LocalGPT.BusinessObjects;

/// <summary>
/// Represents a council model request lease application type, grouping the state and behavior that belong to that domain concept.
/// </summary>
/// <param name="plan">Plan value supplied to the council model request lease operation and used when producing its result.</param>
/// <param name="revision">Revision value supplied to the council model request lease operation and used when producing its result.</param>
/// <param name="isEnabled">Value indicating whether is enabled should apply to this operation.</param>
/// <param name="release">Release value supplied to the council model request lease operation and used when producing its result.</param>
internal sealed class CouncilModelRequestLease(
    CouncilHardwareRoadPlan plan,
    long revision,
    bool isEnabled,
    Action? release) : ICouncilModelRequestLease
{
    /// <summary>
    /// Stores the internal release action state used by <see cref="CouncilModelRequestLease"/> while executing its surrounding workflow.
    /// </summary>
    private Action? releaseAction = release;

    /// <summary>
    /// Gets the plan value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The plan value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public CouncilHardwareRoadPlan Plan { get; } = plan;
    /// <summary>
    /// Gets the revision value that forms part of the council model request lease state consumed or produced by the surrounding workflow.
    /// </summary>
    /// <value>The revision value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public long Revision { get; } = revision;
    /// <summary>
    /// Gets a value indicating whether enabled applies to the council model request lease state.
    /// </summary>
    /// <value>The is enabled value exposed by <see cref="CouncilModelRequestLease"/>.</value>
    public bool IsEnabled { get; } = isEnabled;

    /// <summary>
    /// Releases resources owned by <see cref="CouncilModelRequestLease"/> and leaves the council model request lease workflow in a safely disposed state.
    /// </summary>
    public void Dispose() => Interlocked.Exchange(ref releaseAction, null)?.Invoke();
}
